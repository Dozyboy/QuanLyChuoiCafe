using QuanLyChuoiCafe.Data;
using QuanLyChuoiCafe.Domain.Discounts;
using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Events;
using QuanLyChuoiCafe.Domain.Exceptions;
using QuanLyChuoiCafe.Domain.Services;

namespace QuanLyChuoiCafe.Tests.BanHang;

/// <summary>Test tích hợp: dịch vụ bán hàng chạy trên CSDL SQLite thật (file tạm).</summary>
public sealed class SalesIntegrationTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"cafe_test_{Guid.NewGuid():N}.db");
    private readonly SqliteDatabase _db;
    private readonly FakeStockHandler _stock = new();
    private readonly SalesService _sales;
    private readonly ShiftService _shifts;
    private readonly ProductRepository _products;
    private readonly CustomerRepository _customers;
    private readonly User _manager;

    public SalesIntegrationTests()
    {
        _db = new SqliteDatabase(_path);
        _db.Initialize(Path.Combine(AppContext.BaseDirectory, "Database"));
        var shiftRepository = new ShiftRepository(_db);
        var publisher = new OrderEventPublisher();
        publisher.Subscribe(_stock);
        _sales = new SalesService(new SqliteUnitOfWorkFactory(_db), shiftRepository, publisher, () => Sample.Now);
        _shifts = new ShiftService(shiftRepository, () => Sample.Now);
        _products = new ProductRepository(_db);
        _customers = new CustomerRepository(_db);
        _manager = new AuthService(new UserRepository(_db)).Login("quanly", "Demo@123");
    }

    public void Dispose() => File.Delete(_path);

    /// <summary>Đơn 4 ly cà phê sữa M (29.000 đ) của khách mẫu có 40 điểm.</summary>
    private Order DraftOrder()
    {
        var order = _sales.NewOrder(_manager);
        var product = _products.Search(null, "Cà phê sữa", true).Single();
        order.AddItem(product, product.Sizes[0], 4);
        order.AssignCustomer(_customers.FindByPhone("0900000002").Id);
        return order;
    }

    private long Count(string table)
    {
        using var connection = _db.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return (long)command.ExecuteScalar();
    }

    [Fact]
    public void Checkout_saves_order_adds_points_and_notifies_stock()
    {
        _shifts.Open(_manager, 500_000);
        var order = DraftOrder();
        order.ApplyDiscount(new PercentDiscount("10%", 10, 100_000));

        _sales.Checkout(_manager, order, PaymentMethod.Cash, 200_000);

        Assert.Equal(1, Count("orders"));
        Assert.Equal(50, _customers.FindByPhone("0900000002").Points); // 40 + 104.400 / 10.000
        Assert.Equal(4, _stock.Received.Single().Items.Single().Quantity);
        Assert.Equal(104_400, _shifts.Close(_manager, 604_400).CashSales);
    }

    [Fact]
    public void Checkout_rolls_back_everything_when_stock_handler_fails()
    {
        _shifts.Open(_manager, 0);
        _stock.OutOfStock = true;
        var order = DraftOrder();

        Assert.Throws<ValidationException>(() => _sales.Checkout(_manager, order, PaymentMethod.Cash, 200_000));

        Assert.Equal(0, Count("orders"));
        Assert.Equal(0, Count("order_lines"));
        Assert.Equal(0, Count("payments"));
        Assert.Equal(40, _customers.FindByPhone("0900000002").Points);
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Cancel_revokes_points_and_removes_cash_from_shift()
    {
        var shift = _shifts.Open(_manager, 0);
        var order = DraftOrder();
        _sales.Checkout(_manager, order, PaymentMethod.Cash, 116_000);

        _sales.Cancel(_manager, order.Id, shift.Id, "Khách đổi ý");

        Assert.Equal(40, _customers.FindByPhone("0900000002").Points);
        Assert.Equal(0, _shifts.Close(_manager, 0).CashSales);
    }

    [Fact]
    public void Cannot_sell_without_open_shift() =>
        Assert.Throws<ShiftException>(() => _sales.NewOrder(_manager));

    [Fact]
    public void Login_with_wrong_password_is_rejected() =>
        Assert.Throws<AuthenticationException>(() => new AuthService(new UserRepository(_db)).Login("quanly", "sai"));
}

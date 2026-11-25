using QuanLyChuoiCafe.Domain.Entities;

namespace QuanLyChuoiCafe.Domain.Repositories;

// Các cổng truy cập dữ liệu. Domain chỉ biết giao diện; tầng Data cài đặt (đảo ngược phụ thuộc).

public interface IUserRepository
{
    User FindByUsername(string username);
}

public interface IProductRepository
{
    IReadOnlyList<Category> GetCategories();
    /// <summary>Lọc theo nhóm và tên; tham số null hoặc rỗng nghĩa là không lọc.</summary>
    IReadOnlyList<Product> Search(int? categoryId, string keyword, bool activeOnly);
    void Add(Product product);
    void Update(Product product);
    void AddSize(ProductSize size);
    void UpdateSize(ProductSize size);
}

public interface IPromotionRepository
{
    IReadOnlyList<Promotion> GetValidOn(DateTime date);
}

public interface ICustomerRepository
{
    Customer FindByPhone(string phone);
}

public interface IShiftRepository
{
    Shift FindOpenByCashier(int cashierId);
    void Add(Shift shift);
    void Close(Shift shift);
    /// <summary>Tổng tiền mặt của các đơn đã thanh toán (chưa hủy) trong ca.</summary>
    decimal GetCashSales(int shiftId);
}

public interface IOrderWriter
{
    /// <summary>Lưu đơn, các dòng hàng, thanh toán; gán Id cho đơn.</summary>
    void Add(Order order);
    OrderSummary FindSummary(int orderId);
    void MarkCancelled(int orderId, string reason, DateTime cancelledAt);
    void AddCustomerPoints(int customerId, int points);
}

public sealed record OrderSummary(int Id, int BranchId, int ShiftId, int? CustomerId, OrderStatus Status,
    decimal Total, IReadOnlyList<Events.SoldItem> Items);

/// <summary>Một giao dịch CSDL: không gọi Commit thì mọi thay đổi bị hoàn tác khi Dispose.</summary>
public interface IUnitOfWork : IDisposable
{
    IOrderWriter Orders { get; }
    void Commit();
}

public interface IUnitOfWorkFactory
{
    IUnitOfWork Begin();
}

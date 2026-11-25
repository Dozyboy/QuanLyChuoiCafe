using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Events;
using QuanLyChuoiCafe.Domain.Exceptions;
using QuanLyChuoiCafe.Domain.Repositories;

namespace QuanLyChuoiCafe.Tests.BanHang;

/// <summary>Dữ liệu mẫu dùng chung cho các test bán hàng.</summary>
internal static class Sample
{
    public static readonly DateTime Now = new(2026, 12, 1, 9, 0, 0);

    public static Order Order(params (decimal Price, int Quantity)[] lines)
    {
        var order = new Order(branchId: 1, shiftId: 1, cashierId: 2);
        for (var i = 0; i < lines.Length; i++)
        {
            var product = new Product(i + 1, $"Món {i + 1}", 1);
            order.AddItem(product, new ProductSize(i + 1, i + 1, "M", lines[i].Price), lines[i].Quantity);
        }
        return order;
    }

    public static User Cashier() => new(2, "thungan", "Thu ngân", "x", Role.Cashier, 1);
    public static User StockKeeper() => new(3, "thukho", "Thủ kho", "x", Role.StockKeeper, 1);
}

internal class FakeShiftRepository : IShiftRepository
{
    public Shift Open { get; set; }
    public decimal CashSales { get; set; }

    public Shift FindOpenByCashier(int cashierId) =>
        Open is { IsOpen: true } && Open.CashierId == cashierId ? Open : null;

    public void Add(Shift shift)
    {
        shift.Id = 1;
        Open = shift;
    }

    public void Close(Shift shift) { }
    public decimal GetCashSales(int shiftId) => CashSales;
}

/// <summary>Đứng thay phân hệ Kho (TV2): ghi lại sự kiện, hoặc giả lập thiếu nguyên liệu.</summary>
internal class FakeStockHandler : IOrderPaidHandler
{
    public bool OutOfStock { get; set; }
    public List<OrderPaidEvent> Received { get; } = new();

    public void OnOrderPaid(OrderPaidEvent e, IUnitOfWork unitOfWork)
    {
        if (OutOfStock) throw new ValidationException("Không đủ nguyên liệu.");
        Received.Add(e);
    }
}

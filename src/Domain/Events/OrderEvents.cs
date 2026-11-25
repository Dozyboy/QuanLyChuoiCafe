using QuanLyChuoiCafe.Domain.Repositories;

namespace QuanLyChuoiCafe.Domain.Events;

// Mẫu Observer, cũng là HỢP ĐỒNG ghép nối giữa phân hệ Bán hàng (TV1) và Kho (TV2).
// Bán hàng chỉ phát sự kiện, không biết gì về bảng hay lớp của Kho.

public sealed record SoldItem(int ProductSizeId, int Quantity);

public sealed record OrderPaidEvent(int OrderId, int BranchId, DateTime PaidAt, IReadOnlyList<SoldItem> Items);

public sealed record OrderCancelledEvent(int OrderId, int BranchId, DateTime CancelledAt, string Reason,
    IReadOnlyList<SoldItem> Items);

/// <summary>
/// Được gọi TRONG giao dịch thanh toán. Ném DomainException thì toàn bộ đơn bị hoàn tác
/// (ví dụ: không đủ nguyên liệu).
/// </summary>
public interface IOrderPaidHandler
{
    void OnOrderPaid(OrderPaidEvent e, IUnitOfWork unitOfWork);
}

/// <summary>Được gọi trong giao dịch hủy đơn.</summary>
public interface IOrderCancelledHandler
{
    void OnOrderCancelled(OrderCancelledEvent e, IUnitOfWork unitOfWork);
}

/// <summary>Chủ thể (Subject) của mẫu Observer.</summary>
public class OrderEventPublisher
{
    private readonly List<IOrderPaidHandler> _paidHandlers = new();
    private readonly List<IOrderCancelledHandler> _cancelledHandlers = new();

    public void Subscribe(IOrderPaidHandler handler)
    {
        if (!_paidHandlers.Contains(handler)) _paidHandlers.Add(handler);
    }

    public void Subscribe(IOrderCancelledHandler handler)
    {
        if (!_cancelledHandlers.Contains(handler)) _cancelledHandlers.Add(handler);
    }

    public void Unsubscribe(IOrderPaidHandler handler) => _paidHandlers.Remove(handler);

    public void Unsubscribe(IOrderCancelledHandler handler) => _cancelledHandlers.Remove(handler);

    public void Publish(OrderPaidEvent e, IUnitOfWork unitOfWork)
    {
        foreach (var handler in _paidHandlers.ToList()) handler.OnOrderPaid(e, unitOfWork);
    }

    public void Publish(OrderCancelledEvent e, IUnitOfWork unitOfWork)
    {
        foreach (var handler in _cancelledHandlers.ToList()) handler.OnOrderCancelled(e, unitOfWork);
    }
}

using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Events;
using QuanLyChuoiCafe.Domain.Exceptions;
using QuanLyChuoiCafe.Domain.Repositories;

namespace QuanLyChuoiCafe.Domain.Services;

/// <summary>Use case bán hàng, thanh toán và hủy đơn.</summary>
public class SalesService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IShiftRepository _shifts;
    private readonly OrderEventPublisher _publisher;
    private readonly Func<DateTime> _clock;

    public SalesService(IUnitOfWorkFactory unitOfWorkFactory, IShiftRepository shifts,
        OrderEventPublisher publisher, Func<DateTime> clock)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _shifts = shifts;
        _publisher = publisher;
        _clock = clock;
    }

    /// <summary>Quy tắc: chỉ được bán khi đang mở ca.</summary>
    public Order NewOrder(User cashier)
    {
        cashier.Demand(Permission.Sell);
        var shift = _shifts.FindOpenByCashier(cashier.Id)
                    ?? throw new ShiftException("Hãy mở ca trước khi bán hàng.");
        return new Order(shift.BranchId, shift.Id, cashier.Id);
    }

    /// <summary>
    /// Lưu đơn, cộng điểm và phát sự kiện trong CÙNG một giao dịch: một bước lỗi thì không bước nào được ghi.
    /// </summary>
    public Payment Checkout(User cashier, Order order, PaymentMethod method, decimal amountReceived)
    {
        cashier.Demand(Permission.Sell);
        var shift = _shifts.FindOpenByCashier(cashier.Id);
        if (shift == null || shift.Id != order.ShiftId)
            throw new ShiftException("Đơn không thuộc ca đang mở của bạn.");

        order.Pay(method, amountReceived, _clock());
        try
        {
            using var unitOfWork = _unitOfWorkFactory.Begin();
            unitOfWork.Orders.Add(order);
            if (order.CustomerId is int customerId)
                unitOfWork.Orders.AddCustomerPoints(customerId, Customer.PointsFor(order.Total));

            var items = order.Lines.Select(l => new SoldItem(l.ProductSizeId, l.Quantity)).ToList();
            _publisher.Publish(new OrderPaidEvent(order.Id, order.BranchId, order.Payment.PaidAt, items), unitOfWork);
            unitOfWork.Commit();
        }
        catch
        {
            order.UndoPayment();
            order.Id = 0;
            throw;
        }
        return order.Payment;
    }

    /// <summary>Quy tắc: chỉ người có quyền hủy, phải có lý do, đơn đã thanh toán trong ca còn mở.</summary>
    public void Cancel(User manager, int orderId, int openShiftId, string reason)
    {
        manager.Demand(Permission.CancelOrder);
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("Hãy ghi lý do hủy đơn.");

        using var unitOfWork = _unitOfWorkFactory.Begin();
        var summary = unitOfWork.Orders.FindSummary(orderId)
                      ?? throw new ValidationException("Không tìm thấy đơn hàng.");
        if (summary.Status != OrderStatus.Paid) throw new ValidationException("Chỉ hủy được đơn đã thanh toán.");
        if (summary.ShiftId != openShiftId) throw new ShiftException("Chỉ hủy được đơn của ca đang mở.");

        var now = _clock();
        unitOfWork.Orders.MarkCancelled(orderId, reason.Trim(), now);
        if (summary.CustomerId is int customerId)
            unitOfWork.Orders.AddCustomerPoints(customerId, -Customer.PointsFor(summary.Total));
        _publisher.Publish(new OrderCancelledEvent(orderId, summary.BranchId, now, reason.Trim(), summary.Items), unitOfWork);
        unitOfWork.Commit();
    }
}

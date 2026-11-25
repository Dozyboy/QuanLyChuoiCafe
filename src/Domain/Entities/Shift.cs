using QuanLyChuoiCafe.Domain.Exceptions;

namespace QuanLyChuoiCafe.Domain.Entities;

/// <summary>Ca làm việc của một thu ngân tại một chi nhánh.</summary>
public class Shift
{
    public Shift(int id, int branchId, int cashierId, DateTime openedAt, decimal openingCash,
        DateTime? closedAt = null, decimal? countedCash = null)
    {
        if (openingCash < 0) throw new ShiftException("Tiền đầu ca không được âm.");
        Id = id;
        BranchId = branchId;
        CashierId = cashierId;
        OpenedAt = openedAt;
        OpeningCash = openingCash;
        ClosedAt = closedAt;
        CountedCash = countedCash;
    }

    public int Id { get; set; }
    public int BranchId { get; }
    public int CashierId { get; }
    public DateTime OpenedAt { get; }
    public decimal OpeningCash { get; }
    public DateTime? ClosedAt { get; private set; }
    public decimal? CountedCash { get; private set; }
    public bool IsOpen => ClosedAt == null;

    /// <summary>Đóng ca và trả về chênh lệch: tiền đếm được trừ (tiền đầu ca + tiền mặt bán được).</summary>
    public decimal Close(decimal countedCash, decimal cashSales, DateTime closedAt)
    {
        if (!IsOpen) throw new ShiftException("Ca đã đóng.");
        if (countedCash < 0) throw new ShiftException("Tiền đếm cuối ca không được âm.");
        ClosedAt = closedAt;
        CountedCash = countedCash;
        return countedCash - (OpeningCash + cashSales);
    }
}

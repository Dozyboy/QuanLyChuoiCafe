using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Exceptions;
using QuanLyChuoiCafe.Domain.Repositories;

namespace QuanLyChuoiCafe.Domain.Services;

public sealed record ShiftReport(Shift Shift, decimal CashSales, decimal Difference);

public class ShiftService
{
    private readonly IShiftRepository _shifts;
    private readonly Func<DateTime> _clock;

    public ShiftService(IShiftRepository shifts, Func<DateTime> clock)
    {
        _shifts = shifts;
        _clock = clock;
    }

    public Shift Current(User user) => _shifts.FindOpenByCashier(user.Id);

    public Shift Open(User user, decimal openingCash)
    {
        user.Demand(Permission.ManageShift);
        if (Current(user) != null) throw new ShiftException("Bạn đang có một ca chưa đóng.");

        var shift = new Shift(0, user.BranchId, user.Id, _clock(), openingCash);
        _shifts.Add(shift);
        return shift;
    }

    public ShiftReport Close(User user, decimal countedCash)
    {
        user.Demand(Permission.ManageShift);
        var shift = Current(user) ?? throw new ShiftException("Không có ca nào đang mở.");

        var cashSales = _shifts.GetCashSales(shift.Id);
        var difference = shift.Close(countedCash, cashSales, _clock());
        _shifts.Close(shift);
        return new ShiftReport(shift, cashSales, difference);
    }
}

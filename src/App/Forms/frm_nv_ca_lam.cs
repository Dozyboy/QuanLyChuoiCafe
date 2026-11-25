using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Services;

namespace QuanLyChuoiCafe.App.Forms;

/// <summary>Form nghiệp vụ mở ca, đóng ca và đối soát tiền mặt.</summary>
public class frm_nv_ca_lam : Form
{
    private readonly ShiftService _shifts;
    private readonly User _user;
    private readonly Label _state = Ui.Label("", bold: true);
    private readonly NumericUpDown _cash = new() { Maximum = 1_000_000_000, Increment = 10_000, ThousandsSeparator = true, Width = 180 };
    private readonly Label _cashLabel = Ui.Label("");
    private readonly Button _open;
    private readonly Button _close;
    private readonly Label _report = Ui.Label("");

    public frm_nv_ca_lam(ShiftService shifts, User user)
    {
        _shifts = shifts;
        _user = user;
        SuspendLayout();
        Ui.Prepare(this, "Ca làm việc", 700, 400);

        _open = Ui.Button("Mở ca", (_, _) => OpenShift(), primary: true);
        _close = Ui.Button("Đóng ca và đối soát", (_, _) => CloseShift(), primary: true);

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(16)
        };
        layout.Controls.AddRange(new Control[] { _state, _cashLabel, _cash, _open, _close, _report });
        Controls.Add(layout);
        ResumeLayout();
        Load += (_, _) => RefreshState();
    }

    private void RefreshState()
    {
        var shift = _shifts.Current(_user);
        _state.Text = shift == null
            ? "Bạn chưa mở ca."
            : $"Ca số {shift.Id} mở lúc {shift.OpenedAt:HH:mm dd/MM/yyyy}, tiền đầu ca {shift.OpeningCash:#,##0} đ.";
        _cashLabel.Text = shift == null ? "Tiền mặt đầu ca (đ)" : "Tiền mặt đếm được cuối ca (đ)";
        _open.Visible = shift == null;
        _close.Visible = shift != null;
    }

    private void OpenShift()
    {
        if (Ui.Try(() => _shifts.Open(_user, _cash.Value)))
        {
            _cash.Value = 0;
            _report.Text = "";
        }
        RefreshState();
    }

    private void CloseShift()
    {
        var answer = MessageBox.Show("Đóng ca hiện tại?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        Ui.Try(() =>
        {
            var report = _shifts.Close(_user, _cash.Value);
            _report.Text = $"Tiền đầu ca: {report.Shift.OpeningCash:#,##0} đ\n" +
                           $"Tiền mặt bán được: {report.CashSales:#,##0} đ\n" +
                           $"Tiền đếm được: {report.Shift.CountedCash:#,##0} đ\n" +
                           $"Chênh lệch: {report.Difference:#,##0} đ";
        });
        RefreshState();
    }
}

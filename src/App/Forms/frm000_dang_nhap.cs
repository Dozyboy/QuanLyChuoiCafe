using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Services;

namespace QuanLyChuoiCafe.App.Forms;

public class frm000_dang_nhap : Form
{
    private readonly AuthService _auth;
    private readonly TextBox _username = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right };
    private readonly TextBox _password = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };

    public frm000_dang_nhap(AuthService auth)
    {
        _auth = auth;
        SuspendLayout();
        Ui.Prepare(this, "Đăng nhập", 440, 250);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(24, 16, 24, 16) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var title = Ui.Label("QUẢN LÝ CHUỖI CÀ PHÊ - TRÀ SỮA", bold: true);
        title.ForeColor = Ui.Primary;
        layout.Controls.Add(title, 0, 0);
        layout.SetColumnSpan(title, 2);
        layout.Controls.Add(Ui.Label("Tên đăng nhập"), 0, 1);
        layout.Controls.Add(_username, 1, 1);
        layout.Controls.Add(Ui.Label("Mật khẩu"), 0, 2);
        layout.Controls.Add(_password, 1, 2);

        var login = Ui.Button("Đăng nhập", (_, _) => Login(), primary: true);
        var exit = Ui.Button("Thoát", (_, _) => DialogResult = DialogResult.Cancel);
        var buttons = Ui.Row(login, exit);
        layout.Controls.Add(buttons, 1, 3);
        AcceptButton = login;
        CancelButton = exit;

        Controls.Add(layout);
        ResumeLayout();
    }

    public User LoggedInUser { get; private set; }

    internal void Fill(string username, string password)
    {
        _username.Text = username;
        _password.Text = password;
    }

    private void Login()
    {
        var ok = Ui.Try(() => LoggedInUser = _auth.Login(_username.Text, _password.Text));
        if (ok) DialogResult = DialogResult.OK;
        else _password.SelectAll();
    }
}

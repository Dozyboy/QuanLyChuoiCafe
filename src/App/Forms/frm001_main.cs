using QuanLyChuoiCafe.Domain.Entities;

namespace QuanLyChuoiCafe.App.Forms;

/// <summary>Khung chính dùng chung: menu theo quyền, form chức năng nhúng ở vùng giữa.</summary>
public class frm001_main : Form
{
    private readonly Panel _content = new() { Dock = DockStyle.Fill, Padding = new Padding(8) };
    private Form _current;

    public frm001_main(AppServices services, User user)
    {
        SuspendLayout();
        Ui.Prepare(this, "Quản lý chuỗi cà phê - trà sữa", 1180, 700);
        MinimumSize = new Size(1000, 620);

        var menu = new FlowLayoutPanel
        {
            Dock = DockStyle.Left, Width = 190, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, BackColor = Ui.Soft, Padding = new Padding(8)
        };
        // Quyền quyết định mục nào xuất hiện; tầng Domain vẫn kiểm tra lại khi thực hiện.
        AddMenu(menu, user, Permission.Sell, "Bán hàng", () => new frm_nv_ban_hang(services, user));
        AddMenu(menu, user, Permission.ManageShift, "Ca làm việc", () => new frm_nv_ca_lam(services.Shifts, user));
        AddMenu(menu, user, Permission.ManageCatalog, "Danh mục món", () => new frm_dm_san_pham(services.Products, user));

        var logout = Ui.Button("Đăng xuất", (_, _) => { LogoutRequested = true; Close(); });
        logout.MinimumSize = new Size(166, 38);
        menu.Controls.Add(logout);

        var status = new StatusStrip();
        status.Items.Add($"Người dùng: {user.FullName}   |   Vai trò: {user.Role.Name}   |   Chi nhánh số {user.BranchId}");

        Controls.Add(_content);
        Controls.Add(menu);
        Controls.Add(status);
        ResumeLayout();
    }

    public bool LogoutRequested { get; private set; }

    /// <summary>Nhúng một form chức năng vào vùng nội dung, đóng form đang mở.</summary>
    internal void Open(Form form)
    {
        _current?.Dispose();
        _current = form;
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        _content.Controls.Add(form);
        form.Show();
    }

    private void AddMenu(Control menu, User user, Permission permission, string text, Func<Form> create)
    {
        if (!user.Role.Allows(permission)) return;
        var button = Ui.Button(text, (_, _) => Open(create()), primary: true);
        button.MinimumSize = new Size(166, 42);
        menu.Controls.Add(button);
    }
}

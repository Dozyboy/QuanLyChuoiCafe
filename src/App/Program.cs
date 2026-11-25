using QuanLyChuoiCafe.App.Forms;
using QuanLyChuoiCafe.Domain.Entities;

namespace QuanLyChuoiCafe.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // Chụp ảnh các form cho slide và báo cáo, dùng CSDL tạm: QuanLyChuoiCafe.exe --screenshot <thư mục>
        if (args.Length == 2 && args[0] == "--screenshot")
        {
            Screenshots.Capture(args[1]);
            return;
        }

        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.Message, "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);

        AppServices services;
        try
        {
            services = new AppServices(AppServices.ReadDatabasePath());
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không mở được cơ sở dữ liệu: " + ex.Message, "Lỗi khởi động",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Đăng xuất thì quay lại form đăng nhập.
        while (true)
        {
            User user;
            using (var login = new frm000_dang_nhap(services.Auth))
            {
                if (login.ShowDialog() != DialogResult.OK) return;
                user = login.LoggedInUser;
            }

            using var main = new frm001_main(services, user);
            Application.Run(main);
            if (!main.LogoutRequested) return;
        }
    }
}

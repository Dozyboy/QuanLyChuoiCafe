using QuanLyChuoiCafe.App.Forms;
using QuanLyChuoiCafe.Domain.Entities;

namespace QuanLyChuoiCafe.App;

/// <summary>Vẽ từng form ra file PNG bằng dữ liệu mẫu trong một CSDL tạm.</summary>
internal static class Screenshots
{
    public static void Capture(string folder)
    {
        Directory.CreateDirectory(folder);
        var database = Path.Combine(Path.GetTempPath(), $"cafe_shot_{Guid.NewGuid():N}.db");
        try
        {
            var services = new AppServices(database);

            using (var login = new frm000_dang_nhap(services.Auth))
            {
                login.Fill("thungan", "Demo@123");
                Save(login, folder, "frm000_dang_nhap");
            }

            var manager = services.Auth.Login("quanly", "Demo@123");
            services.Shifts.Open(manager, 500_000);

            Embedded(services, manager, folder, "frm_nv_ban_hang",
                () => new frm_nv_ban_hang(services, manager), f => ((frm_nv_ban_hang)f).FillSample());
            Embedded(services, manager, folder, "frm_dm_san_pham",
                () => new frm_dm_san_pham(services.Products, manager), _ => { });
            Embedded(services, manager, folder, "frm_nv_ca_lam",
                () => new frm_nv_ca_lam(services.Shifts, manager), _ => { });
        }
        finally
        {
            File.Delete(database);
        }
    }

    private static void Embedded(AppServices services, User user, string folder, string name,
        Func<Form> create, Action<Form> prepare)
    {
        using var main = new frm001_main(services, user);
        Show(main);
        var child = create();
        main.Open(child);
        Application.DoEvents();
        prepare(child);
        Save(main, folder, name);
    }

    private static void Show(Form form)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(0, 0);
        form.Show();
        Application.DoEvents();
    }

    private static void Save(Form form, string folder, string name)
    {
        if (!form.Visible) Show(form);
        form.Refresh();
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.Combine(folder, name + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        form.Hide();
    }
}

using System.Text.Json;
using QuanLyChuoiCafe.Data;
using QuanLyChuoiCafe.Domain.Events;
using QuanLyChuoiCafe.Domain.Repositories;
using QuanLyChuoiCafe.Domain.Services;

namespace QuanLyChuoiCafe.App;

/// <summary>Nơi duy nhất tạo và ghép các đối tượng (composition root). Form nhận phụ thuộc qua constructor.</summary>
public class AppServices
{
    public AppServices(string databasePath)
    {
        var database = new SqliteDatabase(databasePath);
        database.Initialize(Path.Combine(AppContext.BaseDirectory, "Database"));

        Products = new ProductRepository(database);
        Promotions = new PromotionRepository(database);
        Customers = new CustomerRepository(database);
        var shifts = new ShiftRepository(database);

        // Phân hệ Kho (TV2) sẽ đăng ký handler trừ kho vào publisher tại đây.
        Publisher = new OrderEventPublisher();

        Auth = new AuthService(new UserRepository(database));
        Shifts = new ShiftService(shifts, () => DateTime.Now);
        Sales = new SalesService(new SqliteUnitOfWorkFactory(database), shifts, Publisher, () => DateTime.Now);
    }

    public IProductRepository Products { get; }
    public IPromotionRepository Promotions { get; }
    public ICustomerRepository Customers { get; }
    public OrderEventPublisher Publisher { get; }
    public AuthService Auth { get; }
    public ShiftService Shifts { get; }
    public SalesService Sales { get; }

    /// <summary>Đường dẫn CSDL đọc từ appsettings.json; đường dẫn tương đối tính từ thư mục chương trình.</summary>
    public static string ReadDatabasePath()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        using var json = JsonDocument.Parse(File.ReadAllText(file));
        var path = json.RootElement.GetProperty("database").GetString();
        return Path.GetFullPath(path, AppContext.BaseDirectory);
    }
}

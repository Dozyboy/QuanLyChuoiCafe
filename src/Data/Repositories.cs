using Microsoft.Data.Sqlite;
using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Exceptions;
using QuanLyChuoiCafe.Domain.Repositories;

namespace QuanLyChuoiCafe.Data;

public class UserRepository : IUserRepository
{
    private readonly SqliteDatabase _db;
    public UserRepository(SqliteDatabase db) => _db = db;

    public User FindByUsername(string username)
    {
        using var connection = _db.Open();
        return connection.Command(null,
                "SELECT id, username, full_name, password_hash, role_id, branch_id, is_active FROM users WHERE username = $u",
                ("$u", username))
            .ReadAll(r => new User(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3),
                Role.FromId(r.GetInt32(4)), r.GetInt32(5), r.GetBoolean(6)))
            .FirstOrDefault();
    }
}

public class ProductRepository : IProductRepository
{
    private readonly SqliteDatabase _db;
    public ProductRepository(SqliteDatabase db) => _db = db;

    public IReadOnlyList<Category> GetCategories()
    {
        using var connection = _db.Open();
        return connection.Command(null, "SELECT id, name FROM categories ORDER BY id")
            .ReadAll(r => new Category(r.GetInt32(0), r.GetString(1)));
    }

    public IReadOnlyList<Product> Search(int? categoryId, string keyword, bool activeOnly)
    {
        using var connection = _db.Open();
        // Một truy vấn JOIN duy nhất, tránh N+1. Lọc tên ở C# để không phân biệt hoa thường tiếng Việt.
        var rows = connection.Command(null, @"
                SELECT p.id, p.name, p.category_id, p.is_active, s.id, s.name, s.price
                FROM products p LEFT JOIN product_sizes s ON s.product_id = p.id
                WHERE ($category IS NULL OR p.category_id = $category) AND ($activeOnly = 0 OR p.is_active = 1)
                ORDER BY p.category_id, p.id, s.price",
                ("$category", categoryId), ("$activeOnly", activeOnly ? 1 : 0))
            .ReadAll(r => (
                ProductId: r.GetInt32(0), Name: r.GetString(1), CategoryId: r.GetInt32(2), Active: r.GetBoolean(3),
                SizeId: r.IsDBNull(4) ? (int?)null : r.GetInt32(4),
                SizeName: r.IsDBNull(5) ? null : r.GetString(5),
                Price: r.IsDBNull(6) ? 0 : r.GetInt64(6)));

        var products = new List<Product>();
        foreach (var group in rows.GroupBy(x => x.ProductId))
        {
            var first = group.First();
            if (!string.IsNullOrWhiteSpace(keyword) &&
                !first.Name.Contains(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase)) continue;

            var product = new Product(first.ProductId, first.Name, first.CategoryId, first.Active);
            foreach (var row in group.Where(x => x.SizeId != null))
                product.AddSize(new ProductSize(row.SizeId.Value, product.Id, row.SizeName, row.Price));
            products.Add(product);
        }
        return products;
    }

    public void Add(Product product) => product.Id = Insert(
        "INSERT INTO products (name, category_id, is_active) VALUES ($n, $c, $a)",
        $"Đã có món tên \"{product.Name}\".",
        ("$n", product.Name), ("$c", product.CategoryId), ("$a", product.IsActive ? 1 : 0));

    public void Update(Product product) => Execute(
        "UPDATE products SET name = $n, category_id = $c, is_active = $a WHERE id = $id",
        $"Đã có món tên \"{product.Name}\".",
        ("$n", product.Name), ("$c", product.CategoryId), ("$a", product.IsActive ? 1 : 0), ("$id", product.Id));

    public void AddSize(ProductSize size) => size.Id = Insert(
        "INSERT INTO product_sizes (product_id, name, price) VALUES ($p, $n, $price)",
        $"Món này đã có size \"{size.Name}\".",
        ("$p", size.ProductId), ("$n", size.Name), ("$price", (long)size.Price));

    public void UpdateSize(ProductSize size) => Execute(
        "UPDATE product_sizes SET name = $n, price = $price WHERE id = $id",
        $"Món này đã có size \"{size.Name}\".",
        ("$n", size.Name), ("$price", (long)size.Price), ("$id", size.Id));

    private int Insert(string sql, string duplicateMessage, params (string, object)[] parameters) =>
        (int)Execute(sql + "; SELECT last_insert_rowid();", duplicateMessage, parameters);

    private long Execute(string sql, string duplicateMessage, params (string, object)[] parameters)
    {
        const int constraintViolation = 19;
        try
        {
            using var connection = _db.Open();
            using var command = connection.Command(null, sql, parameters);
            return Convert.ToInt64(command.ExecuteScalar() ?? 0L);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == constraintViolation)
        {
            throw new ValidationException(duplicateMessage);
        }
    }
}

public class PromotionRepository : IPromotionRepository
{
    private readonly SqliteDatabase _db;
    public PromotionRepository(SqliteDatabase db) => _db = db;

    public IReadOnlyList<Promotion> GetValidOn(DateTime date)
    {
        using var connection = _db.Open();
        return connection.Command(null,
                "SELECT id, name, type, value, min_order, date_from, date_to FROM promotions ORDER BY id")
            .ReadAll(r => new Promotion(r.GetInt32(0), r.GetString(1), (PromotionType)r.GetInt32(2),
                r.GetInt64(3), r.GetInt64(4), DateTime.Parse(r.GetString(5)), DateTime.Parse(r.GetString(6))))
            .Where(p => p.IsValidOn(date))
            .ToList();
    }
}

public class CustomerRepository : ICustomerRepository
{
    private readonly SqliteDatabase _db;
    public CustomerRepository(SqliteDatabase db) => _db = db;

    public Customer FindByPhone(string phone)
    {
        using var connection = _db.Open();
        return connection.Command(null, "SELECT id, name, phone, points FROM customers WHERE phone = $p",
                ("$p", phone))
            .ReadAll(r => new Customer(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetInt32(3)))
            .FirstOrDefault();
    }
}

public class ShiftRepository : IShiftRepository
{
    private readonly SqliteDatabase _db;
    public ShiftRepository(SqliteDatabase db) => _db = db;

    public Shift FindOpenByCashier(int cashierId)
    {
        using var connection = _db.Open();
        return connection.Command(null, @"
                SELECT id, branch_id, cashier_id, opened_at, opening_cash FROM shifts
                WHERE cashier_id = $c AND closed_at IS NULL", ("$c", cashierId))
            .ReadAll(r => new Shift(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2),
                SqliteDatabase.ToDateTime(r.GetString(3)), r.GetInt64(4)))
            .FirstOrDefault();
    }

    public void Add(Shift shift)
    {
        using var connection = _db.Open();
        using var command = connection.Command(null, @"
            INSERT INTO shifts (branch_id, cashier_id, opened_at, opening_cash) VALUES ($b, $c, $o, $cash);
            SELECT last_insert_rowid();",
            ("$b", shift.BranchId), ("$c", shift.CashierId),
            ("$o", SqliteDatabase.ToText(shift.OpenedAt)), ("$cash", (long)shift.OpeningCash));
        shift.Id = Convert.ToInt32(command.ExecuteScalar());
    }

    public void Close(Shift shift)
    {
        using var connection = _db.Open();
        using var command = connection.Command(null,
            "UPDATE shifts SET closed_at = $at, counted_cash = $cash WHERE id = $id AND closed_at IS NULL",
            ("$at", SqliteDatabase.ToText(shift.ClosedAt.Value)), ("$cash", (long)shift.CountedCash.Value),
            ("$id", shift.Id));
        if (command.ExecuteNonQuery() != 1) throw new ShiftException("Ca đã được đóng ở nơi khác.");
    }

    public decimal GetCashSales(int shiftId)
    {
        using var connection = _db.Open();
        using var command = connection.Command(null, @"
            SELECT COALESCE(SUM(p.amount_due), 0) FROM payments p JOIN orders o ON o.id = p.order_id
            WHERE o.shift_id = $s AND o.status = $paid AND p.method = $cash",
            ("$s", shiftId), ("$paid", (int)OrderStatus.Paid), ("$cash", (int)PaymentMethod.Cash));
        return Convert.ToInt64(command.ExecuteScalar());
    }
}

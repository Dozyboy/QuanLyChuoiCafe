using System.Globalization;
using Microsoft.Data.Sqlite;
using QuanLyChuoiCafe.Domain.Services;

namespace QuanLyChuoiCafe.Data;

/// <summary>Mở kết nối, tạo lược đồ và dữ liệu mẫu. Mọi truy vấn trong tầng này đều dùng tham số.</summary>
public class SqliteDatabase
{
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    private readonly string _connectionString;

    public SqliteDatabase(string path)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            ForeignKeys = true,
            Pooling = false
        }.ToString();
    }

    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    /// <summary>Chạy mọi file schema_*.sql rồi seed_*.sql trong thư mục, sau đó tạo tài khoản demo.</summary>
    public void Initialize(string scriptFolder)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        foreach (var pattern in new[] { "schema_*.sql", "seed_*.sql" })
        {
            foreach (var file in Directory.GetFiles(scriptFolder, pattern).OrderBy(f => f))
                connection.Command(transaction, File.ReadAllText(file)).ExecuteNonQuery();
        }
        CreateDemoUsers(connection, transaction);
        transaction.Commit();
    }

    /// <summary>Tài khoản chỉ dùng để chạy thử trên máy cá nhân (xem README).</summary>
    private static void CreateDemoUsers(SqliteConnection connection, SqliteTransaction transaction)
    {
        var demo = new (string Username, string FullName, string Password, int RoleId)[]
        {
            ("quanly", "Quản lý Demo", "Demo@123", 1),
            ("thungan", "Thu ngân Demo", "Demo@123", 2),
            ("thukho", "Thủ kho Demo", "Demo@123", 3)
        };

        foreach (var user in demo)
        {
            var exists = connection.Command(transaction, "SELECT COUNT(*) FROM users WHERE username = $u",
                ("$u", user.Username)).ExecuteScalar();
            if (Convert.ToInt64(exists) > 0) continue;

            connection.Command(transaction,
                "INSERT INTO users (username, full_name, password_hash, role_id, branch_id) VALUES ($u, $n, $h, $r, 1)",
                ("$u", user.Username), ("$n", user.FullName),
                ("$h", PasswordHasher.Hash(user.Password)), ("$r", user.RoleId)).ExecuteNonQuery();
        }
    }

    internal static string ToText(DateTime value) => value.ToString(DateTimeFormat, CultureInfo.InvariantCulture);

    internal static DateTime ToDateTime(string value) =>
        DateTime.ParseExact(value, DateTimeFormat, CultureInfo.InvariantCulture);
}

internal static class SqliteExtensions
{
    public static SqliteCommand Command(this SqliteConnection connection, SqliteTransaction transaction,
        string sql, params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    /// <summary>Đọc toàn bộ kết quả và ánh xạ từng dòng.</summary>
    public static List<T> ReadAll<T>(this SqliteCommand command, Func<SqliteDataReader, T> map)
    {
        using (command)
        using (var reader = command.ExecuteReader())
        {
            var result = new List<T>();
            while (reader.Read()) result.Add(map(reader));
            return result;
        }
    }
}

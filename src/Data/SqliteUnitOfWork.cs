using Microsoft.Data.Sqlite;
using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Events;
using QuanLyChuoiCafe.Domain.Repositories;

namespace QuanLyChuoiCafe.Data;

public class SqliteUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly SqliteDatabase _db;
    public SqliteUnitOfWorkFactory(SqliteDatabase db) => _db = db;
    public IUnitOfWork Begin() => new SqliteUnitOfWork(_db);
}

/// <summary>
/// Một giao dịch ACID. Handler của phân hệ khác (trong tầng Data) dùng lại Connection và Transaction
/// này để ghi dữ liệu cùng giao dịch với đơn hàng.
/// </summary>
public sealed class SqliteUnitOfWork : IUnitOfWork
{
    private bool _committed;

    internal SqliteUnitOfWork(SqliteDatabase db)
    {
        Connection = db.Open();
        Transaction = Connection.BeginTransaction();
        Orders = new OrderWriter(Connection, Transaction);
    }

    internal SqliteConnection Connection { get; }
    internal SqliteTransaction Transaction { get; }
    public IOrderWriter Orders { get; }

    public void Commit()
    {
        Transaction.Commit();
        _committed = true;
    }

    public void Dispose()
    {
        if (!_committed) Transaction.Rollback();
        Transaction.Dispose();
        Connection.Dispose();
    }
}

internal class OrderWriter : IOrderWriter
{
    private readonly SqliteConnection _connection;
    private readonly SqliteTransaction _transaction;

    public OrderWriter(SqliteConnection connection, SqliteTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public void Add(Order order)
    {
        order.Id = Convert.ToInt32(Command(@"
            INSERT INTO orders (shift_id, customer_id, status, discount_name, discount)
            VALUES ($shift, $customer, $status, $discountName, $discount);
            SELECT last_insert_rowid();",
            ("$shift", order.ShiftId), ("$customer", order.CustomerId), ("$status", (int)order.Status),
            ("$discountName", order.DiscountName), ("$discount", (long)order.DiscountAmount)).ExecuteScalar());

        foreach (var line in order.Lines)
        {
            Command(@"INSERT INTO order_lines (order_id, product_size_id, unit_price, quantity)
                      VALUES ($order, $size, $price, $quantity)",
                ("$order", order.Id), ("$size", line.ProductSizeId), ("$price", (long)line.UnitPrice),
                ("$quantity", line.Quantity)).ExecuteNonQuery();
        }

        var payment = order.Payment;
        Command(@"INSERT INTO payments (order_id, method, amount_due, amount_received, paid_at)
                  VALUES ($order, $method, $due, $received, $at)",
            ("$order", order.Id), ("$method", (int)payment.Method), ("$due", (long)payment.AmountDue),
            ("$received", (long)payment.AmountReceived), ("$at", SqliteDatabase.ToText(payment.PaidAt))).ExecuteNonQuery();
    }

    public OrderSummary FindSummary(int orderId)
    {
        var header = Command(@"
                SELECT o.shift_id, s.branch_id, o.customer_id, o.status, p.amount_due
                FROM orders o JOIN shifts s ON s.id = o.shift_id JOIN payments p ON p.order_id = o.id
                WHERE o.id = $id", ("$id", orderId))
            .ReadAll(r => (ShiftId: r.GetInt32(0), BranchId: r.GetInt32(1),
                CustomerId: r.IsDBNull(2) ? (int?)null : r.GetInt32(2),
                Status: (OrderStatus)r.GetInt32(3), Total: r.GetInt64(4)));
        if (header.Count == 0) return null;

        var items = Command("SELECT product_size_id, quantity FROM order_lines WHERE order_id = $id", ("$id", orderId))
            .ReadAll(r => new SoldItem(r.GetInt32(0), r.GetInt32(1)));
        var h = header[0];
        return new OrderSummary(orderId, h.BranchId, h.ShiftId, h.CustomerId, h.Status, h.Total, items);
    }

    public void MarkCancelled(int orderId, string reason, DateTime cancelledAt) =>
        Command("UPDATE orders SET status = $status, cancel_reason = $reason, cancelled_at = $at WHERE id = $id",
            ("$status", (int)OrderStatus.Cancelled), ("$reason", reason),
            ("$at", SqliteDatabase.ToText(cancelledAt)), ("$id", orderId)).ExecuteNonQuery();

    public void AddCustomerPoints(int customerId, int points) =>
        Command("UPDATE customers SET points = MAX(points + $points, 0) WHERE id = $id",
            ("$points", points), ("$id", customerId)).ExecuteNonQuery();

    private SqliteCommand Command(string sql, params (string, object)[] parameters) =>
        _connection.Command(_transaction, sql, parameters);
}

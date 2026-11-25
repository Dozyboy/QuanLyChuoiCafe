using QuanLyChuoiCafe.Domain.Discounts;
using QuanLyChuoiCafe.Domain.Exceptions;

namespace QuanLyChuoiCafe.Domain.Entities;

public enum OrderStatus { Draft = 0, Paid = 1, Cancelled = 2 }

public enum PaymentMethod { Cash = 0, BankTransfer = 1 }

/// <summary>Dòng hàng: lưu giá tại thời điểm bán, đổi giá thực đơn không làm đổi hóa đơn cũ.</summary>
public class OrderLine
{
    public const int MaxQuantity = 100;

    public OrderLine(int productSizeId, string productName, string sizeName, decimal unitPrice, int quantity)
    {
        if (unitPrice <= 0) throw new ValidationException("Đơn giá phải lớn hơn 0.");
        ProductSizeId = productSizeId;
        ProductName = productName;
        SizeName = sizeName;
        UnitPrice = unitPrice;
        SetQuantity(quantity);
    }

    public int ProductSizeId { get; }
    public string ProductName { get; }
    public string SizeName { get; }
    public decimal UnitPrice { get; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    internal void SetQuantity(int quantity)
    {
        if (quantity < 1 || quantity > MaxQuantity)
            throw new ValidationException($"Số lượng mỗi món phải từ 1 đến {MaxQuantity}.");
        Quantity = quantity;
    }
}

public class Payment
{
    public Payment(PaymentMethod method, decimal amountDue, decimal amountReceived, DateTime paidAt)
    {
        if (amountReceived < amountDue)
            throw new ValidationException($"Khách đưa {amountReceived:#,##0} đ, còn thiếu {amountDue - amountReceived:#,##0} đ.");
        if (method == PaymentMethod.BankTransfer && amountReceived != amountDue)
            throw new ValidationException("Chuyển khoản phải đúng số tiền cần trả.");
        Method = method;
        AmountDue = amountDue;
        AmountReceived = amountReceived;
        PaidAt = paidAt;
    }

    public PaymentMethod Method { get; }
    public decimal AmountDue { get; }
    public decimal AmountReceived { get; }
    public decimal Change => AmountReceived - AmountDue;
    public DateTime PaidAt { get; }
}

/// <summary>Đơn hàng. Chỉ sửa được khi còn là bản nháp.</summary>
public class Order
{
    private readonly List<OrderLine> _lines = new();
    private IDiscountStrategy _discount = new NoDiscount();

    public Order(int branchId, int shiftId, int cashierId)
    {
        BranchId = branchId;
        ShiftId = shiftId;
        CashierId = cashierId;
    }

    public int Id { get; set; }
    public int BranchId { get; }
    public int ShiftId { get; }
    public int CashierId { get; }
    public int? CustomerId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public Payment Payment { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines;
    public string DiscountName => _discount.Name;

    public decimal Subtotal => _lines.Sum(l => l.LineTotal);

    /// <summary>Số tiền giảm luôn nằm trong [0, tạm tính] dù chiến lược trả về giá trị nào.</summary>
    public decimal DiscountAmount => Math.Clamp(decimal.Truncate(_discount.CalculateDiscount(this)), 0, Subtotal);

    public decimal Total => Subtotal - DiscountAmount;

    public void AddItem(Product product, ProductSize size, int quantity)
    {
        EnsureDraft();
        if (!product.IsActive) throw new ValidationException($"Món \"{product.Name}\" đã ngừng bán.");
        if (size.ProductId != product.Id) throw new ValidationException("Size không thuộc món đã chọn.");

        var existing = _lines.FirstOrDefault(l => l.ProductSizeId == size.Id);
        if (existing != null)
            existing.SetQuantity(existing.Quantity + quantity);
        else
            _lines.Add(new OrderLine(size.Id, product.Name, size.Name, size.Price, quantity));
    }

    public void ChangeQuantity(int productSizeId, int quantity)
    {
        EnsureDraft();
        FindLine(productSizeId).SetQuantity(quantity);
    }

    public void RemoveItem(int productSizeId)
    {
        EnsureDraft();
        _lines.Remove(FindLine(productSizeId));
    }

    public void ApplyDiscount(IDiscountStrategy discount)
    {
        EnsureDraft();
        _discount = discount ?? new NoDiscount();
    }

    public void AssignCustomer(int? customerId)
    {
        EnsureDraft();
        CustomerId = customerId;
    }

    public void Pay(PaymentMethod method, decimal amountReceived, DateTime paidAt)
    {
        EnsureDraft();
        if (_lines.Count == 0) throw new ValidationException("Đơn hàng chưa có món nào.");
        Payment = new Payment(method, Total, amountReceived, paidAt);
        Status = OrderStatus.Paid;
    }

    /// <summary>Trả đơn về bản nháp khi giao dịch lưu thất bại.</summary>
    internal void UndoPayment()
    {
        Payment = null;
        Status = OrderStatus.Draft;
    }

    private OrderLine FindLine(int productSizeId) =>
        _lines.FirstOrDefault(l => l.ProductSizeId == productSizeId)
        ?? throw new ValidationException("Không tìm thấy món trong đơn.");

    private void EnsureDraft()
    {
        if (Status != OrderStatus.Draft)
            throw new ValidationException("Đơn đã thanh toán hoặc đã hủy, không được sửa.");
    }
}

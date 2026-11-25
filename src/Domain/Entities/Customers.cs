using QuanLyChuoiCafe.Domain.Exceptions;

namespace QuanLyChuoiCafe.Domain.Entities;

public class Customer
{
    /// <summary>Mỗi 10.000 đ thanh toán được 1 điểm.</summary>
    public const decimal AmountPerPoint = 10_000m;

    public Customer(int id, string name, string phone, int points = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ValidationException("Tên khách hàng không được để trống.");
        if (string.IsNullOrWhiteSpace(phone) || phone.Length != 10 || !phone.All(char.IsDigit))
            throw new ValidationException("Số điện thoại phải gồm đúng 10 chữ số.");
        Id = id;
        Name = name.Trim();
        Phone = phone;
        Points = points;
    }

    public int Id { get; }
    public string Name { get; }
    public string Phone { get; }
    public int Points { get; }

    public static int PointsFor(decimal paidAmount) => (int)(paidAmount / AmountPerPoint);
}

public enum PromotionType { Percent = 0, FixedAmount = 1, BuyXGetOne = 2 }

/// <summary>Chương trình khuyến mãi lưu trong CSDL; cách tính do chiến lược tương ứng đảm nhận.</summary>
public class Promotion
{
    public Promotion(int id, string name, PromotionType type, decimal value, decimal minOrder, DateTime from, DateTime to)
    {
        if (to.Date < from.Date) throw new ValidationException("Ngày kết thúc phải sau ngày bắt đầu.");
        Id = id;
        Name = name;
        Type = type;
        Value = value;
        MinOrder = minOrder;
        From = from.Date;
        To = to.Date;
    }

    public int Id { get; }
    public string Name { get; }
    public PromotionType Type { get; }
    public decimal Value { get; }
    public decimal MinOrder { get; }
    public DateTime From { get; }
    public DateTime To { get; }

    public bool IsValidOn(DateTime date) => date.Date >= From && date.Date <= To;

    public override string ToString() => Name;
}

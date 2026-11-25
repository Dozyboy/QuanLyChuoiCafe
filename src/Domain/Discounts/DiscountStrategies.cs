using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Exceptions;

namespace QuanLyChuoiCafe.Domain.Discounts;

// Mẫu Strategy: Order là Context, IDiscountStrategy là Strategy, các lớp dưới là ConcreteStrategy.
// Thêm kiểu khuyến mãi mới = thêm một lớp, không sửa Order (nguyên tắc đóng/mở).

public interface IDiscountStrategy
{
    string Name { get; }
    decimal CalculateDiscount(Order order);
}

public sealed class NoDiscount : IDiscountStrategy
{
    public string Name => "Không khuyến mãi";
    public decimal CalculateDiscount(Order order) => 0;
}

/// <summary>Giảm theo phần trăm khi đơn đạt giá trị tối thiểu.</summary>
public sealed class PercentDiscount : IDiscountStrategy
{
    private readonly decimal _percent;
    private readonly decimal _minOrder;

    public PercentDiscount(string name, decimal percent, decimal minOrder = 0)
    {
        if (percent <= 0 || percent > 100) throw new ValidationException("Phần trăm giảm phải trong khoảng (0, 100].");
        Name = name;
        _percent = percent;
        _minOrder = minOrder;
    }

    public string Name { get; }

    public decimal CalculateDiscount(Order order) =>
        order.Subtotal >= _minOrder ? order.Subtotal * _percent / 100 : 0;
}

/// <summary>Giảm một số tiền cố định khi đơn đạt giá trị tối thiểu.</summary>
public sealed class FixedAmountDiscount : IDiscountStrategy
{
    private readonly decimal _amount;
    private readonly decimal _minOrder;

    public FixedAmountDiscount(string name, decimal amount, decimal minOrder = 0)
    {
        if (amount <= 0) throw new ValidationException("Số tiền giảm phải lớn hơn 0.");
        Name = name;
        _amount = amount;
        _minOrder = minOrder;
    }

    public string Name { get; }

    public decimal CalculateDiscount(Order order) => order.Subtotal >= _minOrder ? _amount : 0;
}

/// <summary>Mua X ly tặng 1: cứ đủ X+1 ly thì ly rẻ nhất trong nhóm được miễn phí.</summary>
public sealed class BuyXGetOneDiscount : IDiscountStrategy
{
    private readonly int _buy;

    public BuyXGetOneDiscount(string name, int buy)
    {
        if (buy < 1) throw new ValidationException("Số ly cần mua phải từ 1.");
        Name = name;
        _buy = buy;
    }

    public string Name { get; }

    public decimal CalculateDiscount(Order order)
    {
        var cups = order.Lines
            .SelectMany(l => Enumerable.Repeat(l.UnitPrice, l.Quantity))
            .OrderBy(price => price)
            .ToList();
        var freeCups = cups.Count / (_buy + 1);
        return cups.Take(freeCups).Sum();
    }
}

/// <summary>Chọn chiến lược theo chương trình khuyến mãi đang lưu trong CSDL.</summary>
public static class DiscountSelector
{
    public static IDiscountStrategy For(Promotion promotion, DateTime today)
    {
        if (promotion == null) return new NoDiscount();
        if (!promotion.IsValidOn(today)) throw new ValidationException($"\"{promotion.Name}\" không áp dụng cho hôm nay.");

        return promotion.Type switch
        {
            PromotionType.Percent => new PercentDiscount(promotion.Name, promotion.Value, promotion.MinOrder),
            PromotionType.FixedAmount => new FixedAmountDiscount(promotion.Name, promotion.Value, promotion.MinOrder),
            PromotionType.BuyXGetOne => new BuyXGetOneDiscount(promotion.Name, (int)promotion.Value),
            _ => throw new ValidationException("Kiểu khuyến mãi chưa được hỗ trợ.")
        };
    }
}

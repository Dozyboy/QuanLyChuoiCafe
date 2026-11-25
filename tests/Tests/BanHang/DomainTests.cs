using QuanLyChuoiCafe.Domain.Discounts;
using QuanLyChuoiCafe.Domain.Entities;
using QuanLyChuoiCafe.Domain.Events;
using QuanLyChuoiCafe.Domain.Exceptions;
using QuanLyChuoiCafe.Domain.Services;

namespace QuanLyChuoiCafe.Tests.BanHang;

public class DiscountTests
{
    [Theory]
    [InlineData(99_000, 0)]        // ngay dưới ngưỡng
    [InlineData(100_000, 10_000)]  // đúng ngưỡng
    [InlineData(200_000, 20_000)]
    public void Percent_discount_applies_from_minimum_order(int price, int expected)
    {
        var order = Sample.Order((price, 1));
        order.ApplyDiscount(new PercentDiscount("10%", 10, 100_000));
        Assert.Equal(expected, order.DiscountAmount);
    }

    [Fact]
    public void Discount_never_exceeds_subtotal()
    {
        var order = Sample.Order((15_000, 1));
        order.ApplyDiscount(new FixedAmountDiscount("Giảm 20k", 20_000));
        Assert.Equal(15_000, order.DiscountAmount);
        Assert.Equal(0, order.Total);
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(3, 25_000)]
    [InlineData(6, 55_000)]  // 2 ly miễn phí: hai ly rẻ nhất 25.000 + 30.000
    public void Buy_two_get_one_frees_cheapest_cups(int cups, int expected)
    {
        var order = cups == 6
            ? Sample.Order((25_000, 1), (30_000, 1), (40_000, 4))
            : Sample.Order((25_000, 1), (40_000, cups - 1));
        order.ApplyDiscount(new BuyXGetOneDiscount("Mua 2 tặng 1", 2));
        Assert.Equal(expected, order.DiscountAmount);
    }

    [Fact]
    public void Expired_promotion_is_rejected()
    {
        var promotion = new Promotion(1, "Cũ", PromotionType.Percent, 10, 0,
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        Assert.Throws<ValidationException>(() => DiscountSelector.For(promotion, Sample.Now));
    }
}

public class OrderTests
{
    [Fact]
    public void Adding_same_size_merges_lines()
    {
        var product = new Product(1, "Cà phê", 1);
        var size = new ProductSize(1, 1, "M", 25_000);
        var order = new Order(1, 1, 1);
        order.AddItem(product, size, 1);
        order.AddItem(product, size, 2);

        Assert.Single(order.Lines);
        Assert.Equal(75_000, order.Subtotal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Quantity_outside_1_to_100_is_rejected(int quantity) =>
        Assert.Throws<ValidationException>(() => Sample.Order((25_000, quantity)));

    [Fact]
    public void Line_keeps_price_at_time_of_sale()
    {
        var product = new Product(1, "Cà phê", 1);
        var size = new ProductSize(1, 1, "M", 25_000);
        var order = new Order(1, 1, 1);
        order.AddItem(product, size, 1);
        size.ChangePrice(30_000);

        Assert.Equal(25_000, order.Subtotal);
    }

    [Fact]
    public void Cash_below_total_is_rejected_and_order_stays_draft()
    {
        var order = Sample.Order((25_000, 2));
        Assert.Throws<ValidationException>(() => order.Pay(PaymentMethod.Cash, 40_000, Sample.Now));
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Paid_order_returns_change_and_cannot_be_edited()
    {
        var order = Sample.Order((25_000, 2));
        order.Pay(PaymentMethod.Cash, 100_000, Sample.Now);

        Assert.Equal(50_000, order.Payment.Change);
        Assert.Throws<ValidationException>(() => order.RemoveItem(1));
    }
}

public class ShiftAndAccessTests
{
    [Fact]
    public void Cashier_cannot_open_two_shifts()
    {
        var service = new ShiftService(new FakeShiftRepository(), () => Sample.Now);
        service.Open(Sample.Cashier(), 500_000);
        Assert.Throws<ShiftException>(() => service.Open(Sample.Cashier(), 500_000));
    }

    [Fact]
    public void Closing_shift_reports_cash_difference()
    {
        var repository = new FakeShiftRepository { CashSales = 200_000 };
        var service = new ShiftService(repository, () => Sample.Now);
        service.Open(Sample.Cashier(), 500_000);

        Assert.Equal(-10_000, service.Close(Sample.Cashier(), 690_000).Difference);
    }

    [Fact]
    public void Stock_keeper_is_not_allowed_to_sell() =>
        Assert.Throws<PermissionDeniedException>(() => Sample.StockKeeper().Demand(Permission.Sell));

    [Fact]
    public void Password_hash_verifies_only_the_right_password()
    {
        var hash = PasswordHasher.Hash("mat-khau-thu");
        Assert.True(PasswordHasher.Verify("mat-khau-thu", hash));
        Assert.False(PasswordHasher.Verify("sai", hash));
        Assert.NotEqual(hash, PasswordHasher.Hash("mat-khau-thu"));
    }
}

public class PublisherTests
{
    [Fact]
    public void Subscribed_handler_receives_event_once_and_stops_after_unsubscribe()
    {
        var publisher = new OrderEventPublisher();
        var handler = new FakeStockHandler();
        publisher.Subscribe(handler);
        publisher.Subscribe(handler);
        var e = new OrderPaidEvent(1, 1, Sample.Now, new[] { new SoldItem(1, 2) });

        publisher.Publish(e, null);
        publisher.Unsubscribe(handler);
        publisher.Publish(e, null);

        Assert.Single(handler.Received);
    }
}

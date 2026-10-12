using Sahaf.Models;
using Sahaf.Utility;

namespace Sahaf.Tests;

public class PriceCalculatorTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(50, 100)]
    [InlineData(51, 90)]
    [InlineData(100, 90)]
    [InlineData(101, 80)]
    [InlineData(1000, 80)]
    public void UnitPrice_uses_the_right_tier_at_every_boundary(int count, decimal expected) =>
        Assert.Equal(expected, PriceCalculator.UnitPrice(count, 100m, 90m, 80m));

    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(7, 7)]
    [InlineData(5000, PriceCalculator.MaxCount)]
    public void ClampCount_keeps_quantities_in_range(int count, int expected) =>
        Assert.Equal(expected, PriceCalculator.ClampCount(count));

    [Theory]
    [InlineData("150.50", 15050)]
    [InlineData("0.01", 1)]
    [InlineData("150.505", 15051)]
    [InlineData("99999.99", 9999999)]
    public void ToMinorUnits_converts_lira_to_kurus(string amount, long expected) =>
        Assert.Equal(expected, PriceCalculator.ToMinorUnits(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
}

public class OrderWorkflowTests
{
    [Theory]
    [InlineData(SD.StatusPending, false, false, true)]
    [InlineData(SD.StatusApproved, true, true, true)]
    [InlineData(SD.StatusInProcess, false, true, true)]
    [InlineData(SD.StatusShipped, false, false, false)]
    [InlineData(SD.StatusCancelled, false, false, false)]
    public void Transitions_follow_the_order_lifecycle(string status, bool process, bool ship, bool cancel)
    {
        Assert.Equal(process, OrderWorkflow.CanStartProcessing(status));
        Assert.Equal(ship, OrderWorkflow.CanShip(status));
        Assert.Equal(cancel, OrderWorkflow.CanCancel(status));
    }

    [Fact]
    public void Only_card_payments_are_refunded()
    {
        Assert.True(OrderWorkflow.NeedsRefund(SD.PaymentStatusApproved));
        Assert.False(OrderWorkflow.NeedsRefund(SD.PaymentStatusDelayedPayment));
        Assert.False(OrderWorkflow.NeedsRefund(SD.PaymentStatusPending));
    }

    [Fact]
    public void Company_orders_can_be_paid_later_unless_cancelled()
    {
        Assert.True(OrderWorkflow.CanPayLater(SD.StatusShipped, SD.PaymentStatusDelayedPayment));
        Assert.False(OrderWorkflow.CanPayLater(SD.StatusCancelled, SD.PaymentStatusDelayedPayment));
        Assert.False(OrderWorkflow.CanPayLater(SD.StatusApproved, SD.PaymentStatusApproved));
    }
}

public class ProductTests
{
    [Fact]
    public void CopyEditableFrom_never_copies_protected_fields()
    {
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var cover = Guid.NewGuid();
        var stored = new Product { Id = 7, CreatedAt = created, CoverImageId = cover, Title = "Eski" };
        var posted = new Product
        {
            Id = 99, CreatedAt = new DateTime(2000, 1, 1), CoverImageId = Guid.NewGuid(),
            Title = "  Yeni  ", Author = "Yazar", Description = "D", Price = 10, ListPrice = 12,
            Images = [new ProductImage { Data = [1, 2, 3] }],
        };

        stored.CopyEditableFrom(posted);

        Assert.Equal(7, stored.Id);
        Assert.Equal(created, stored.CreatedAt);
        Assert.Equal(cover, stored.CoverImageId);
        Assert.Empty(stored.Images);
        Assert.Equal("Yeni", stored.Title);
        Assert.Equal(10, stored.Price);
    }

    [Theory]
    [InlineData(100, 100, 0)]
    [InlineData(200, 150, 25)]
    [InlineData(90, 100, 0)]
    public void DiscountPercent_is_rounded_and_never_negative(decimal list, decimal price, int expected) =>
        Assert.Equal(expected, new Product { ListPrice = list, Price = price }.DiscountPercent);
}

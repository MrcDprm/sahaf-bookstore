namespace Sahaf.Utility;

// Bulk pricing tiers: 1-50 copies, 51-100 copies, more than 100 copies.
public static class PriceCalculator
{
    public const int MaxCount = 1000;

    public static decimal UnitPrice(int count, decimal price, decimal price50, decimal price100) =>
        count switch
        {
            <= 50 => price,
            <= 100 => price50,
            _ => price100,
        };

    public static int ClampCount(int count) => Math.Clamp(count, 1, MaxCount);

    // Stripe takes amounts in the smallest currency unit (kuruş).
    public static long ToMinorUnits(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}

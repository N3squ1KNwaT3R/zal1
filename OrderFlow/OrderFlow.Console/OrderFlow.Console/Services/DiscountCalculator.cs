namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    private const decimal VipDiscountRate       = 0.10m;
    private const decimal HighValueRate         = 0.05m;
    private const decimal VipHighValueBonus     = 0.05m;
    private const decimal HighValueThreshold    = 1000m;
    private const decimal VipHighValueThreshold = 5000m;
    private const decimal MaxDiscountRate       = 0.25m;

    public decimal Calculate(decimal orderTotal, bool isVip)
    {
        var rate = ComputeRate(orderTotal, isVip);
        return orderTotal * rate;
    }

    private static decimal ComputeRate(decimal orderTotal, bool isVip)
    {
        var rate = 0m;

        if (isVip)
            rate += VipDiscountRate;

        if (orderTotal > HighValueThreshold)
            rate += HighValueRate;

        if (isVip && orderTotal > VipHighValueThreshold)
            rate += VipHighValueBonus;

        return Math.Min(rate, MaxDiscountRate);
    }
}

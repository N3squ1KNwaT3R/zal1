namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    public decimal Calculate(decimal orderTotal, bool isVip)
    {
        decimal rate = 0m;

        if (isVip)
            rate += 0.10m;

        if (orderTotal > 1000m)
            rate += 0.05m;

        if (isVip && orderTotal > 5000m)
            rate += 0.05m;

        rate = Math.Min(rate, 0.25m);

        return orderTotal * rate;
    }
}

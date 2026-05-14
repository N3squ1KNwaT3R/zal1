namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    public decimal Calculate(decimal orderTotal, bool isVip)
    {
        decimal rate = orderTotal switch
        {
            >= 1000m => 0.15m,
            >= 500m  => 0.10m,
            >= 100m  => 0.05m,
            _        => 0m
        };

        if (isVip) rate += 0.05m;

        return orderTotal * rate;
    }
}

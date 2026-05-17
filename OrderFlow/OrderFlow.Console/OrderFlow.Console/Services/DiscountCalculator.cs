namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    public decimal Calculate(decimal orderTotal, bool isVip)
    {
        decimal rate = 0m;

        if (isVip)
            rate += 0.10m;

        return orderTotal * rate;
    }
}

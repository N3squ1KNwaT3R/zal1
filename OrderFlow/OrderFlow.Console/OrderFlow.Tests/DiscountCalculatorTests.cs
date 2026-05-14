using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

// TDD: testy napisane przed implementacją
public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calc = new();

    [Fact]
    public void Below100_NoDiscount()
    {
        var discount = _calc.Calculate(99m, isVip: false);
        Assert.Equal(0m, discount);
    }

    [Fact]
    public void ExactlyZero_NoDiscount()
    {
        Assert.Equal(0m, _calc.Calculate(0m, isVip: false));
    }

    [Theory]
    [InlineData(100,  false, 5.00)]
    [InlineData(499,  false, 24.95)]
    [InlineData(500,  false, 50.00)]
    [InlineData(999,  false, 99.90)]
    [InlineData(1000, false, 150.00)]
    [InlineData(2000, false, 300.00)]
    public void StandardTiers_CorrectDiscount(decimal total, bool isVip, decimal expected)
    {
        Assert.Equal(expected, _calc.Calculate(total, isVip));
    }

    [Fact]
    public void Vip_Below100_FivePercentDiscount()
    {
        // 0% base + 5% VIP = 5% of 50 = 2.5
        Assert.Equal(2.5m, _calc.Calculate(50m, isVip: true));
    }

    [Fact]
    public void Vip_Tier100_TenPercent()
    {
        // 5% base + 5% VIP = 10% of 200 = 20
        Assert.Equal(20m, _calc.Calculate(200m, isVip: true));
    }

    [Fact]
    public void Vip_Tier500_FifteenPercent()
    {
        // 10% base + 5% VIP = 15% of 500 = 75
        Assert.Equal(75m, _calc.Calculate(500m, isVip: true));
    }

    [Fact]
    public void Vip_Tier1000_TwentyPercent()
    {
        // 15% base + 5% VIP = 20% of 2000 = 400
        Assert.Equal(400m, _calc.Calculate(2000m, isVip: true));
    }

    [Fact]
    public void BoundaryAt500_UsesTier500Rate()
    {
        Assert.Equal(50m, _calc.Calculate(500m, isVip: false));
    }

    [Fact]
    public void BoundaryAt1000_UsesTier1000Rate()
    {
        Assert.Equal(150m, _calc.Calculate(1000m, isVip: false));
    }
}

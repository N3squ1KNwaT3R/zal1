using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calc = new();

    [Fact]
    public void Calculate_VipCustomerAbove5000_ReturnsTwentyPercent()
    {
        // Arrange
        decimal total = 6000m;
        bool isVip = true;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(1200m, discount); // 10% VIP + 5% high-value + 5% VIP-high-value bonus = 20% of 6000
    }

    [Fact]
    public void Calculate_StandardCustomerSmallAmount_ReturnsZeroDiscount()
    {
        // Arrange
        decimal total = 500m;
        bool isVip = false;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(0m, discount);
    }

    [Fact]
    public void Calculate_StandardCustomerAbove1000_ReturnsFivePercent()
    {
        // Arrange
        decimal total = 2000m;
        bool isVip = false;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(100m, discount); // 5% of 2000
    }

    [Fact]
    public void Calculate_VipCustomerSmallAmount_ReturnsTenPercent()
    {
        // Arrange
        decimal total = 200m;
        bool isVip = true;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(20m, discount); // 10% of 200
    }
}

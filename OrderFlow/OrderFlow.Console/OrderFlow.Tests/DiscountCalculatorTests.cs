using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calc = new();

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
}

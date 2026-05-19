using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calc = new();

    // ── Reguła 1: standardowy klient, mała kwota → 0% ────────────────────────

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

    // ── Reguła 2: klient VIP → 10% ───────────────────────────────────────────

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

    // ── Reguła 3: zamówienie > 1000 → dodatkowe 5% ───────────────────────────

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
    public void Calculate_VipCustomerAbove1000Below5000_ReturnsFifteenPercent()
    {
        // Arrange – łączy regułę VIP (10%) + high-value (5%)
        decimal total = 3000m;
        bool isVip = true;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(450m, discount); // 15% of 3000
    }

    // ── Reguła 4: VIP + > 5000 → dodatkowe 5% (razem 20%) ───────────────────

    [Fact]
    public void Calculate_VipCustomerAbove5000_ReturnsTwentyPercent()
    {
        // Arrange
        decimal total = 6000m;
        bool isVip = true;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(1200m, discount); // 10% + 5% + 5% = 20% of 6000
    }

    [Fact]
    public void Calculate_StandardCustomerAbove5000_ReturnsFivePercentOnly()
    {
        // Arrange – > 5000, ale nie VIP → brak dodatkowego bonusu VIP
        decimal total = 6000m;
        bool isVip = false;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(300m, discount); // tylko 5% of 6000
    }

    // ── Reguła 5: maksymalny rabat 25% ───────────────────────────────────────

    [Fact]
    public void Calculate_AllRulesApply_DiscountNeverExceedsTwentyFivePercent()
    {
        // Arrange – najlepszy możliwy scenariusz: VIP + > 5000 = 20%
        // Math.Min(0.25) chroni przed przekroczeniem limitu przy przyszłych regułach
        decimal total = 10_000m;
        bool isVip = true;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.True(discount <= total * 0.25m, "Rabat nie może przekroczyć 25%");
        Assert.Equal(2_000m, discount); // 20% of 10000
    }

    // ── Przypadki graniczne ───────────────────────────────────────────────────

    [Fact]
    public void Calculate_ZeroTotal_ReturnsZero()
    {
        // Arrange
        decimal total = 0m;
        bool isVip = true;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(0m, discount);
    }

    [Fact]
    public void Calculate_ExactlyAt1000_NoHighValueBonus()
    {
        // Arrange – granica to > 1000, więc dokładnie 1000 nie kwalifikuje
        decimal total = 1000m;
        bool isVip = false;

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(0m, discount);
    }

    // ── [Theory] – kombinacje VIP × próg kwotowy ─────────────────────────────

    [Theory]
    [InlineData(500,   false,   0)]       // standard, < 1000 → 0%
    [InlineData(500,   true,   50)]       // VIP, < 1000 → 10%
    [InlineData(2000,  false, 100)]       // standard, > 1000 → 5%
    [InlineData(2000,  true,  300)]       // VIP + > 1000 → 15%
    [InlineData(6000,  false, 300)]       // standard, > 5000 → 5%
    [InlineData(6000,  true, 1200)]       // VIP + > 5000 → 20%
    public void Calculate_VariousCombinations_ReturnsCorrectDiscount(
        decimal total, bool isVip, decimal expectedDiscount)
    {
        // Arrange – dane z InlineData

        // Act
        decimal discount = _calc.Calculate(total, isVip);

        // Assert
        Assert.Equal(expectedDiscount, discount);
    }
}

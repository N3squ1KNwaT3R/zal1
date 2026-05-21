using Moq;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

// Testy OrderCurrencyConverter: mockujemy ICurrencyService przez Moq,
// NIE przez HttpMessageHandler — to pokazuje różnicę w podejściu.
public class OrderCurrencyConverterTests
{
    private static Order MakeOrder(decimal unitPrice, int qty = 1) => new()
    {
        Customer  = new Customer { FullName = "Jan Kowalski" },
        CreatedAt = DateTime.Now,
        Status    = OrderStatus.New,
        Items     = [new OrderItem { UnitPrice = unitPrice, Quantity = qty, Product = new Product { Name = "P" } }]
    };

    // 1. Happy path: konwersja PLN → USD ────────────────────────────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_ValidCurrency_ReturnsConvertedAmount()
    {
        // Arrange
        var mockSvc = new Mock<ICurrencyService>();
        mockSvc
            .Setup(s => s.ConvertAsync(400m, "PLN", "USD"))
            .ReturnsAsync(99.69m);

        var converter = new OrderCurrencyConverter(mockSvc.Object);
        var order = MakeOrder(unitPrice: 400m, qty: 1); // TotalAmount = 400 PLN

        // Act
        var result = await converter.ConvertOrderTotalAsync(order, "USD");

        // Assert
        Assert.Equal(99.69m, result);
        mockSvc.Verify(s => s.ConvertAsync(400m, "PLN", "USD"), Times.Once);
    }

    // 2. Nieznana waluta → CurrencyServiceException propagowany ─────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_UnknownCurrency_ThrowsCurrencyServiceException()
    {
        // Arrange
        var mockSvc = new Mock<ICurrencyService>();
        mockSvc
            .Setup(s => s.ConvertAsync(It.IsAny<decimal>(), "PLN", "XYZ"))
            .ThrowsAsync(new CurrencyServiceException(404, "Nieznana waluta docelowa: 'XYZ'"));

        var converter = new OrderCurrencyConverter(mockSvc.Object);
        var order = MakeOrder(unitPrice: 200m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CurrencyServiceException>(
            () => converter.ConvertOrderTotalAsync(order, "XYZ"));

        Assert.Equal(404, ex.StatusCode);
    }

    // 3. TotalAmount=0 → wynik również 0 ────────────────────────────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_ZeroTotal_ReturnsZero()
    {
        // Arrange
        var mockSvc = new Mock<ICurrencyService>();
        mockSvc
            .Setup(s => s.ConvertAsync(0m, "PLN", "EUR"))
            .ReturnsAsync(0m);

        var converter = new OrderCurrencyConverter(mockSvc.Object);
        var order = new Order
        {
            Customer  = new Customer { FullName = "Test" },
            CreatedAt = DateTime.Now,
            Status    = OrderStatus.New,
            Items     = [] // TotalAmount = 0
        };

        // Act
        var result = await converter.ConvertOrderTotalAsync(order, "EUR");

        // Assert
        Assert.Equal(0m, result);
    }

    // 4. Weryfikacja: ConvertAsync wołane z TotalAmount zamówienia ────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_PassesCorrectAmountToService()
    {
        // Arrange
        var mockSvc = new Mock<ICurrencyService>();
        mockSvc
            .Setup(s => s.ConvertAsync(It.IsAny<decimal>(), "PLN", "EUR"))
            .ReturnsAsync((decimal total, string _, string _) => total / 4.25m);

        var converter = new OrderCurrencyConverter(mockSvc.Object);
        var order = MakeOrder(unitPrice: 850m, qty: 2); // TotalAmount = 1700

        // Act
        var result = await converter.ConvertOrderTotalAsync(order, "EUR");

        // Assert
        mockSvc.Verify(s => s.ConvertAsync(1700m, "PLN", "EUR"), Times.Once);
        Assert.Equal(1700m / 4.25m, result);
    }
}

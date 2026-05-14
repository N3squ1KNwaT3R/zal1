using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderValidatorTests
{
    private readonly OrderValidator _validator = new();

    // ── helper ────────────────────────────────────────────────────────────────
    private static Order ValidOrder() => new()
    {
        CustomerId = 1,
        Customer   = new Customer { Id = 1, FullName = "Jan Kowalski" },
        CreatedAt  = DateTime.Now.AddMinutes(-1),
        Status     = OrderStatus.New,
        Items      =
        [
            new OrderItem { Quantity = 2, UnitPrice = 50m, Product = new Product { Name = "P1" } }
        ]
    };

    // ── named rules ───────────────────────────────────────────────────────────

    [Fact]
    public void ValidateAll_OrderWithNoItems_ReturnsHasItemsError()
    {
        // Arrange
        var order = ValidOrder();
        order.Items.Clear();

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("no items"));
    }

    [Fact]
    public void ValidateAll_OrderTotalExceedsLimit_ReturnsExceedsLimitError()
    {
        // Arrange
        var order = ValidOrder();
        order.Items = [new OrderItem { Quantity = 1, UnitPrice = 10_001m, Product = new Product { Name = "X" } }];

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("exceeds limit"));
    }

    [Fact]
    public void ValidateAll_OrderTotalExactlyAtLimit_Passes()
    {
        // Arrange
        var order = ValidOrder();
        order.Items = [new OrderItem { Quantity = 1, UnitPrice = 10_000m, Product = new Product { Name = "X" } }];

        // Act
        var (isValid, _) = _validator.ValidateAll(order);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateAll_ItemWithZeroQuantity_ReturnsQuantitiesError()
    {
        // Arrange
        var order = ValidOrder();
        order.Items[0].Quantity = 0;

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("quantities"));
    }

    [Fact]
    public void ValidateAll_ItemWithNegativeQuantity_ReturnsQuantitiesError()
    {
        // Arrange
        var order = ValidOrder();
        order.Items[0].Quantity = -1;

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("quantities"));
    }

    // ── lambda / Func rules ───────────────────────────────────────────────────

    [Fact]
    public void ValidateAll_CreatedAtInFuture_ReturnsFutureError()
    {
        // Arrange
        var order = ValidOrder();
        order.CreatedAt = DateTime.Now.AddDays(1);

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("future"));
    }

    [Fact]
    public void ValidateAll_StatusCancelled_ReturnsCancelledError()
    {
        // Arrange
        var order = ValidOrder();
        order.Status = OrderStatus.Cancelled;

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
    }

    // ── [Theory] — różne statusy ──────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.New,        true)]
    [InlineData(OrderStatus.Validated,  true)]
    [InlineData(OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Completed,  true)]
    [InlineData(OrderStatus.Cancelled,  false)]
    public void ValidateAll_VariousStatuses_OnlyCancelledFails(OrderStatus status, bool expectedValid)
    {
        // Arrange
        var order = ValidOrder();
        order.Status = status;

        // Act
        var (isValid, _) = _validator.ValidateAll(order);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }

    // ── ValidateAll łączący oba mechanizmy ────────────────────────────────────

    [Fact]
    public void ValidateAll_MultipleViolations_ReturnsAllErrors()
    {
        // Arrange – łamie regułę named (brak pozycji) + regułę lambda (Cancelled)
        var order = ValidOrder();
        order.Items.Clear();
        order.Status = OrderStatus.Cancelled;

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.True(errors.Count >= 2, $"Oczekiwano ≥2 błędów, dostano: {errors.Count}");
        Assert.Contains(errors, e => e.Contains("no items"));
        Assert.Contains(errors, e => e.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateAll_ValidOrder_ReturnsNoErrors()
    {
        // Arrange
        var order = ValidOrder();

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }
}

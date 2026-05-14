using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderValidatorTests
{
    private readonly OrderValidator _validator = new();

    private static Order ValidOrder() => new()
    {
        CustomerId = 1,
        Customer = new Customer { Id = 1, FullName = "Jan Kowalski" },
        CreatedAt = DateTime.Now.AddMinutes(-1),
        Status = OrderStatus.New,
        Items =
        [
            new OrderItem { Quantity = 2, UnitPrice = 50m, Product = new Product { Name = "P1" } }
        ]
    };

    [Fact]
    public void ValidOrder_PassesAllRules()
    {
        var (isValid, errors) = _validator.ValidateAll(ValidOrder());
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void EmptyItems_FailsHasItems()
    {
        var order = ValidOrder();
        order.Items.Clear();

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("no items"));
    }

    [Fact]
    public void TotalExceedsLimit_FailsAmountWithinLimit()
    {
        var order = ValidOrder();
        order.Items = [new OrderItem { Quantity = 1, UnitPrice = 10001m, Product = new Product { Name = "P" } }];

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("exceeds limit"));
    }

    [Fact]
    public void ZeroQuantity_FailsAllQuantitiesPositive()
    {
        var order = ValidOrder();
        order.Items[0].Quantity = 0;

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("quantities"));
    }

    [Fact]
    public void FutureDate_FailsDateRule()
    {
        var order = ValidOrder();
        order.CreatedAt = DateTime.Now.AddDays(1);

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("future"));
    }

    [Fact]
    public void CancelledOrder_FailsCancelledRule()
    {
        var order = ValidOrder();
        order.Status = OrderStatus.Cancelled;

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MultipleViolations_ReturnsAllErrors()
    {
        var order = ValidOrder();
        order.Items.Clear();
        order.Status = OrderStatus.Cancelled;

        var (isValid, errors) = _validator.ValidateAll(order);

        Assert.False(isValid);
        Assert.True(errors.Count >= 2);
    }
}

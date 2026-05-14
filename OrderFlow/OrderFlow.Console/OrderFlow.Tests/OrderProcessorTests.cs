using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderProcessorTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static Customer MakeCustomer(string name = "Test") =>
        new() { FullName = name, City = "Warszawa" };

    private static Order MakeOrder(OrderStatus status, decimal unitPrice, int qty = 1) => new()
    {
        Customer  = MakeCustomer(),
        CreatedAt = DateTime.Now.AddMinutes(-1),
        Status    = status,
        Items     = [new OrderItem { Quantity = qty, UnitPrice = unitPrice, Product = new Product { Name = "P" } }]
    };

    private static OrderProcessor ProcessorWith(params Order[] orders) =>
        new(orders.ToList());

    // ── Filter (Predicate<Order>) ─────────────────────────────────────────────

    [Fact]
    public void Filter_ByStatusNew_ReturnsOnlyNewOrders()
    {
        // Arrange
        var processor = ProcessorWith(
            MakeOrder(OrderStatus.New,       100m),
            MakeOrder(OrderStatus.Completed, 200m),
            MakeOrder(OrderStatus.New,       300m)
        );

        // Act
        var result = processor.Filter(o => o.Status == OrderStatus.New);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal(OrderStatus.New, o.Status));
    }

    [Fact]
    public void Filter_NoMatchingOrders_ReturnsEmptyList()
    {
        // Arrange
        var processor = ProcessorWith(
            MakeOrder(OrderStatus.New, 100m),
            MakeOrder(OrderStatus.New, 200m)
        );

        // Act
        var result = processor.Filter(o => o.Status == OrderStatus.Cancelled);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_ByMinimumTotal_ReturnsOrdersAboveThreshold()
    {
        // Arrange
        var processor = ProcessorWith(
            MakeOrder(OrderStatus.New, 50m),
            MakeOrder(OrderStatus.New, 150m),
            MakeOrder(OrderStatus.New, 300m)
        );

        // Act
        var result = processor.Filter(o => o.TotalAmount >= 100m);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.True(o.TotalAmount >= 100m));
    }

    // ── Project (Func<Order, T>) ──────────────────────────────────────────────

    [Fact]
    public void Project_ToTotalAmount_ReturnsTotalsForAllOrders()
    {
        // Arrange
        var processor = ProcessorWith(
            MakeOrder(OrderStatus.New, 100m, qty: 2),   // 200
            MakeOrder(OrderStatus.New, 300m, qty: 1)    // 300
        );

        // Act
        var totals = processor.Project(o => o.TotalAmount);

        // Assert
        Assert.Equal(2, totals.Count);
        Assert.Contains(200m, totals);
        Assert.Contains(300m, totals);
    }

    [Fact]
    public void Project_ToCustomerName_ReturnsAllNames()
    {
        // Arrange
        var o1 = MakeOrder(OrderStatus.New, 50m); o1.Customer.FullName = "Anna";
        var o2 = MakeOrder(OrderStatus.New, 50m); o2.Customer.FullName = "Bartek";
        var processor = new OrderProcessor([o1, o2]);

        // Act
        var names = processor.Project(o => o.Customer.FullName);

        // Assert
        Assert.Equal(["Anna", "Bartek"], names);
    }

    // ── Aggregate (Func<IEnumerable<Order>, decimal>) ─────────────────────────

    [Fact]
    public void Aggregate_SumOfTotals_ReturnsTotalRevenue()
    {
        // Arrange
        var processor = ProcessorWith(
            MakeOrder(OrderStatus.New, 100m),   // 100
            MakeOrder(OrderStatus.New, 200m),   // 200
            MakeOrder(OrderStatus.New,  50m)    //  50
        );

        // Act
        var total = processor.Aggregate(orders => orders.Sum(o => o.TotalAmount));

        // Assert
        Assert.Equal(350m, total);
    }

    [Fact]
    public void Aggregate_MaxTotal_ReturnsHighestOrderValue()
    {
        // Arrange
        var processor = ProcessorWith(
            MakeOrder(OrderStatus.New,  75m),
            MakeOrder(OrderStatus.New, 500m),
            MakeOrder(OrderStatus.New, 200m)
        );

        // Act
        var max = processor.Aggregate(orders => orders.Max(o => o.TotalAmount));

        // Assert
        Assert.Equal(500m, max);
    }

    [Fact]
    public void Aggregate_OnEmptyList_ReturnsZeroWhenSumming()
    {
        // Arrange
        var processor = new OrderProcessor([]);

        // Act
        var total = processor.Aggregate(orders => orders.Sum(o => o.TotalAmount));

        // Assert
        Assert.Equal(0m, total);
    }

    // ── Chain (filter + sort + topN + action) ─────────────────────────────────

    [Fact]
    public void Chain_Top2ByTotal_InvokesActionForTop2Only()
    {
        // Arrange
        var processor = ProcessorWith(
            MakeOrder(OrderStatus.New,  50m),
            MakeOrder(OrderStatus.New, 300m),
            MakeOrder(OrderStatus.New, 100m),
            MakeOrder(OrderStatus.New, 500m)
        );
        var collected = new List<decimal>();

        // Act
        processor.Chain(
            filter:  o => o.Status == OrderStatus.New,
            sortKey: o => o.TotalAmount,
            topN:    2,
            print:   o => collected.Add(o.TotalAmount)
        );

        // Assert
        Assert.Equal(2, collected.Count);
        Assert.Equal([500m, 300m], collected);
    }
}

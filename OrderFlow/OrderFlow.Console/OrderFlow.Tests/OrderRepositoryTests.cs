using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;

namespace OrderFlow.Tests;

public class OrderRepositoryTests : IDisposable
{
    private readonly OrderFlowContext _ctx;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<OrderFlowContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _ctx = new OrderFlowContext(options);
    }

    public void Dispose() => _ctx.Dispose();

    private static Customer MakeCustomer(string name = "Test User") =>
        new() { FullName = name, City = "Warszawa" };

    private static Product MakeProduct(string name = "Widget", decimal price = 10m) =>
        new() { Name = name, Price = price, Category = "General", Stock = 100 };

    [Fact]
    public async Task AddOrder_CanBeRetrieved()
    {
        var customer = MakeCustomer();
        var product  = MakeProduct();
        _ctx.Customers.Add(customer);
        _ctx.Products.Add(product);
        await _ctx.SaveChangesAsync();

        var order = new Order
        {
            Customer  = customer,
            CreatedAt = DateTime.Now,
            Status    = OrderStatus.New,
            Items     = [new OrderItem { Product = product, Quantity = 2, UnitPrice = product.Price }]
        };
        _ctx.Orders.Add(order);
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Orders.Include(o => o.Items).FirstAsync(o => o.Id == order.Id);
        Assert.Single(loaded.Items);
        Assert.Equal(2, loaded.Items[0].Quantity);
    }

    [Fact]
    public async Task DeleteOrder_RemovesFromDatabase()
    {
        var customer = MakeCustomer();
        var product  = MakeProduct();
        _ctx.Customers.Add(customer);
        _ctx.Products.Add(product);
        await _ctx.SaveChangesAsync();

        var order = new Order { Customer = customer, CreatedAt = DateTime.Now, Status = OrderStatus.New };
        _ctx.Orders.Add(order);
        await _ctx.SaveChangesAsync();

        _ctx.Orders.Remove(order);
        await _ctx.SaveChangesAsync();

        Assert.False(await _ctx.Orders.AnyAsync(o => o.Id == order.Id));
    }

    [Fact]
    public async Task UpdateOrderStatus_PersistsChange()
    {
        var customer = MakeCustomer();
        _ctx.Customers.Add(customer);
        await _ctx.SaveChangesAsync();

        var order = new Order { Customer = customer, CreatedAt = DateTime.Now, Status = OrderStatus.New };
        _ctx.Orders.Add(order);
        await _ctx.SaveChangesAsync();

        order.Status = OrderStatus.Completed;
        await _ctx.SaveChangesAsync();

        var loaded = await _ctx.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, loaded!.Status);
    }

    [Fact]
    public async Task QueryByStatus_ReturnsOnlyMatchingOrders()
    {
        var customer = MakeCustomer();
        _ctx.Customers.Add(customer);
        await _ctx.SaveChangesAsync();

        _ctx.Orders.AddRange(
            new Order { Customer = customer, CreatedAt = DateTime.Now, Status = OrderStatus.New },
            new Order { Customer = customer, CreatedAt = DateTime.Now, Status = OrderStatus.New },
            new Order { Customer = customer, CreatedAt = DateTime.Now, Status = OrderStatus.Completed }
        );
        await _ctx.SaveChangesAsync();

        var newOrders = await _ctx.Orders
            .Where(o => o.Status == OrderStatus.New)
            .ToListAsync();

        Assert.Equal(2, newOrders.Count);
    }

    [Fact]
    public async Task CascadeDelete_RemovesOrderItems()
    {
        var customer = MakeCustomer();
        var product  = MakeProduct();
        _ctx.Customers.Add(customer);
        _ctx.Products.Add(product);
        await _ctx.SaveChangesAsync();

        var order = new Order
        {
            Customer  = customer,
            CreatedAt = DateTime.Now,
            Status    = OrderStatus.New,
            Items     = [new OrderItem { Product = product, Quantity = 1, UnitPrice = 10m }]
        };
        _ctx.Orders.Add(order);
        await _ctx.SaveChangesAsync();

        int orderId = order.Id;
        _ctx.Orders.Remove(order);
        await _ctx.SaveChangesAsync();

        Assert.False(await _ctx.OrderItems.AnyAsync(i => i.OrderId == orderId));
    }
}

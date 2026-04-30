using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OrderFlowContext db)
    {
        if (await db.Products.AnyAsync())
            return;

        var products = new List<Product>
        {
            new() { Name = "Laptop",       Price = 3500m, Category = "Electronics" },
            new() { Name = "Mouse",        Price = 120m,  Category = "Electronics" },
            new() { Name = "Desk",         Price = 800m,  Category = "Furniture"   },
            new() { Name = "C# in Depth",  Price = 95m,   Category = "Books"       },
            new() { Name = "Coffee Maker", Price = 250m,  Category = "Appliances"  },
            new() { Name = "Office Chair", Price = 1200m, Category = "Furniture"   },
        };
        db.Products.AddRange(products);
        await db.SaveChangesAsync();   // IDs assigned after this

        var customers = new List<Customer>
        {
            new() { FullName = "Anna Kowalska",    City = "Warsaw", IsVip = true,  Email = "anna@example.com" },
            new() { FullName = "Piotr Nowak",      City = "Krakow", IsVip = false                             },
            new() { FullName = "Maria Wiśniewska", City = "Warsaw", IsVip = false                             },
            new() { FullName = "Jan Zielinski",    City = "Gdansk", IsVip = true,  Email = "jan@example.com"  },
        };
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();   // IDs assigned after this

        var orders = new List<Order>
        {
            new()
            {
                CustomerId = customers[0].Id,
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Items =
                [
                    new() { ProductId = products[0].Id, UnitPrice = products[0].Price, Quantity = 1 },
                    new() { ProductId = products[1].Id, UnitPrice = products[1].Price, Quantity = 2 },
                ]
            },
            new()
            {
                CustomerId = customers[1].Id,
                Status = OrderStatus.Processing,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                Items =
                [
                    new() { ProductId = products[2].Id, UnitPrice = products[2].Price, Quantity = 1 },
                    new() { ProductId = products[5].Id, UnitPrice = products[5].Price, Quantity = 1 },
                ]
            },
            new()
            {
                CustomerId = customers[2].Id,
                Status = OrderStatus.New,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Items =
                [
                    new() { ProductId = products[3].Id, UnitPrice = products[3].Price, Quantity = 3 },
                ]
            },
            new()
            {
                CustomerId = customers[3].Id,
                Status = OrderStatus.Validated,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Items =
                [
                    new() { ProductId = products[4].Id, UnitPrice = products[4].Price, Quantity = 2 },
                    new() { ProductId = products[1].Id, UnitPrice = products[1].Price, Quantity = 1 },
                ]
            },
            new()
            {
                CustomerId = customers[0].Id,
                Status = OrderStatus.Cancelled,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Items =
                [
                    new() { ProductId = products[0].Id, UnitPrice = products[0].Price, Quantity = 2 },
                ]
            },
            new()
            {
                CustomerId = customers[1].Id,
                Status = OrderStatus.New,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Items =
                [
                    new() { ProductId = products[5].Id, UnitPrice = products[5].Price, Quantity = 1 },
                ]
            },
        };
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
    }
}

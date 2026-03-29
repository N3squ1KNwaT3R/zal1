using OrderFlow.Console.Models;

namespace OrderFlow.Console.Data;

public static class SampleData
{
    public static List<Product> Products => new()
    {
        new() { Id = 1, Name = "Laptop",        Price = 3500m, Category = "Electronics" },
        new() { Id = 2, Name = "Mouse",          Price = 120m,  Category = "Electronics" },
        new() { Id = 3, Name = "Desk",           Price = 800m,  Category = "Furniture"   },
        new() { Id = 4, Name = "C# in Depth",    Price = 95m,   Category = "Books"       },
        new() { Id = 5, Name = "Coffee Maker",   Price = 250m,  Category = "Appliances"  },
        new() { Id = 6, Name = "Office Chair",   Price = 1200m, Category = "Furniture"   },
    };

    public static List<Customer> Customers => new()
    {
        new() { Id = 1, Name = "Anna Kowalska",  City = "Warsaw",  IsVip = true  },
        new() { Id = 2, Name = "Piotr Nowak",    City = "Krakow",  IsVip = false },
        new() { Id = 3, Name = "Maria Wiśniewska", City = "Warsaw", IsVip = false },
        new() { Id = 4, Name = "Jan Zielinski",  City = "Gdansk",  IsVip = true  },
    };

    public static List<Order> Orders
    {
        get
        {
            var p = Products;
            var c = Customers;
            return new()
            {
                new Order
                {
                    Id = 1, Customer = c[0], Status = OrderStatus.Completed,
                    CreatedAt = DateTime.Now.AddDays(-10),
                    Items = new() {
                        new() { Product = p[0], Quantity = 1 },
                        new() { Product = p[1], Quantity = 2 }
                    }
                },
                new Order
                {
                    Id = 2, Customer = c[1], Status = OrderStatus.Processing,
                    CreatedAt = DateTime.Now.AddDays(-3),
                    Items = new() {
                        new() { Product = p[2], Quantity = 1 },
                        new() { Product = p[5], Quantity = 1 }
                    }
                },
                new Order
                {
                    Id = 3, Customer = c[2], Status = OrderStatus.New,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    Items = new() {
                        new() { Product = p[3], Quantity = 3 }
                    }
                },
                new Order
                {
                    Id = 4, Customer = c[3], Status = OrderStatus.Validated,
                    CreatedAt = DateTime.Now.AddDays(-5),
                    Items = new() {
                        new() { Product = p[4], Quantity = 2 },
                        new() { Product = p[1], Quantity = 1 }
                    }
                },
                new Order
                {
                    Id = 5, Customer = c[0], Status = OrderStatus.Cancelled,
                    CreatedAt = DateTime.Now.AddDays(-2),
                    Items = new() {
                        new() { Product = p[0], Quantity = 2 }
                    }
                },
                new Order
                {
                    Id = 6, Customer = c[1], Status = OrderStatus.New,
                    CreatedAt = DateTime.Now.AddDays(-1),
                    Items = new() { } 
                },
            };
        }
    }
}
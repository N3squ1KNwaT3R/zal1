using OrderFlow.Console.Models;

namespace OrderFlow.Console.Data;

public static class SampleData
{
    public static List<Product> Products = new()
    {
        new Product { Id = 1, Name = "Laptop", Price = 4500m, Category = "Electronics" },
        new Product { Id = 2, Name = "Smartphone", Price = 2500m, Category = "Electronics" },
        new Product { Id = 3, Name = "Coffee Maker", Price = 600m, Category = "Appliances" },
        new Product { Id = 4, Name = "Desk Chair", Price = 850m, Category = "Furniture" },
        new Product { Id = 5, Name = "Mechanical Keyboard", Price = 400m, Category = "Electronics" }
    };

    public static List<Customer> Customers = new()
    {
        new Customer { Id = 1, FullName = "Ivan Ivanov", IsVip = true },
        new Customer { Id = 2, FullName = "Anna Smith", IsVip = false },
        new Customer { Id = 3, FullName = "John Doe", IsVip = false },
        new Customer { Id = 4, FullName = "Elena Petrova", IsVip = false }
    };

    public static List<Order> GetOrders()
    {
        return new List<Order>
        {
            new Order { Id = 1, OrderDate = DateTime.Now.AddDays(-5), Status = OrderStatus.Completed, 
                Items = { new OrderItem { Product = Products[0], Quantity = 1 } } },
            new Order { Id = 2, OrderDate = DateTime.Now.AddDays(-3), Status = OrderStatus.Processing, 
                Items = { new OrderItem { Product = Products[2], Quantity = 2 }, new OrderItem { Product = Products[4], Quantity = 1 } } },
            new Order { Id = 3, OrderDate = DateTime.Now.AddDays(-2), Status = OrderStatus.New, 
                Items = { new OrderItem { Product = Products[1], Quantity = 1 } } },
            new Order { Id = 4, OrderDate = DateTime.Now.AddDays(-1), Status = OrderStatus.Cancelled, 
                Items = { new OrderItem { Product = Products[3], Quantity = 1 } } },
            new Order { Id = 5, OrderDate = DateTime.Now, Status = OrderStatus.Validated, 
                Items = { new OrderItem { Product = Products[0], Quantity = 1 }, new OrderItem { Product = Products[1], Quantity = 1 } } },
            new Order { Id = 6, OrderDate = DateTime.Now, Status = OrderStatus.New, 
                Items = { new OrderItem { Product = Products[4], Quantity = 3 } } }
        };
    }

    public static void InitializeData()
    {
        // Привязываем заказы к клиентам для удобства тестов
        var orders = GetOrders();
        Customers[0].Orders.Add(orders[0]); // VIP Ivan получил 1-й заказ
        Customers[0].Orders.Add(orders[4]); // и 5-й
        Customers[1].Orders.Add(orders[1]);
        Customers[2].Orders.Add(orders[2]);
        Customers[3].Orders.Add(orders[3]);
        Customers[3].Orders.Add(orders[5]);
    }
}
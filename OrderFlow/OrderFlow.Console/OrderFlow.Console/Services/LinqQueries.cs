using OrderFlow.Console.Models;
using SysConsole = System.Console;

namespace OrderFlow.Console.Services;

public static class LinqQueries
{
    public static void RunAll(List<Order> orders, List<Customer> customers)
    {
        
        
        SysConsole.WriteLine("\n=== 1. Orders per city (query syntax) ===");
        var byCity =
            from o in orders
            join c in customers on o.Customer.Id equals c.Id
            group o by c.City into g
            select new { City = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) };

        foreach (var x in byCity)
            SysConsole.WriteLine($"  {x.City}: {x.Count} orders, {x.Total:C}");

        
        
        SysConsole.WriteLine("\n=== 2. All ordered products (SelectMany) ===");
        var allProducts = orders
            .SelectMany(o => o.Items, (o, i) => new { OrderId = o.Id, i.Product.Name, i.TotalPrice })
            .OrderBy(x => x.Name);

        foreach (var x in allProducts)
            SysConsole.WriteLine($"  Order#{x.OrderId} — {x.Name}: {x.TotalPrice:C}");

        
        SysConsole.WriteLine("\n=== 3. Top customers by total spend ===");
        var topCustomers = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.Customer.FullName)
            .Select(g => new { Customer = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(x => x.Total);

        foreach (var x in topCustomers)
            SysConsole.WriteLine($"  {x.Customer}: {x.Total:C}");

        
        SysConsole.WriteLine("\n=== 4. Average item price per category ===");
        var avgPerCategory = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.Product.Category)
            .Select(g => new { Category = g.Key, Avg = g.Average(i => i.Product.Price) });

        foreach (var x in avgPerCategory)
            SysConsole.WriteLine($"  {x.Category}: avg {x.Avg:C}");

        
        SysConsole.WriteLine("\n=== 5. All customers with order count (GroupJoin / left join) ===");
        var customerOrders = customers
            .GroupJoin(orders,
                c => c.Id,
                o => o.Customer.Id,
                (c, os) => new { c.FullName, c.IsVip, OrderCount = os.Count(), Total = os.Sum(o => o.TotalAmount) });

        foreach (var x in customerOrders)
            SysConsole.WriteLine($"  {x.FullName} (VIP:{x.IsVip}): {x.OrderCount} orders, {x.Total:C}");

        
        
        SysConsole.WriteLine("\n=== 6. Favourite category per customer (mixed syntax) ===");
        var favCategory =
            from o in orders
            where o.Status != OrderStatus.Cancelled
            group o by o.Customer.FullName into g
            let favourite = g
                .SelectMany(o => o.Items)
                .GroupBy(i => i.Product.Category)
                .OrderByDescending(cat => cat.Count())
                .Select(cat => cat.Key)
                .FirstOrDefault() ?? "N/A"
            select new { Customer = g.Key, Favourite = favourite };

        foreach (var x in favCategory)
            SysConsole.WriteLine($"  {x.Customer}: {x.Favourite}");
    }
}

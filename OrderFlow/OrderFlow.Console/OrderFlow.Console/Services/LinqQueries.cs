using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public static class LinqQueries
{
    public static void RunAll(List<Order> orders, List<Customer> customers)
    {
        // 1. JOIN: orders + customers — группировка по городу (query syntax)
        // Query syntax читается как SQL — удобно для join+groupby
        Console.WriteLine("\n=== 1. Orders per city (query syntax) ===");
        var byCity =
            from o in orders
            join c in customers on o.Customer.Id equals c.Id
            group o by c.City into g
            select new { City = g.Key, Count = g.Count(), Total = g.Sum(o => o.TotalAmount) };

        foreach (var x in byCity)
            Console.WriteLine($"  {x.City}: {x.Count} orders, {x.Total:C}");

        // 2. SelectMany: Order → OrderItems → Product (method syntax)
        // Method syntax лучше для цепочек трансформаций
        Console.WriteLine("\n=== 2. All ordered products (SelectMany) ===");
        var allProducts = orders
            .SelectMany(o => o.Items, (o, i) => new { OrderId = o.Id, i.Product.Name, i.TotalPrice })
            .OrderBy(x => x.Name);

        foreach (var x in allProducts)
            Console.WriteLine($"  Order#{x.OrderId} — {x.Name}: {x.TotalPrice:C}");

        // 3. GroupBy с агрегацией: топ клиенты по сумме (method syntax)
        Console.WriteLine("\n=== 3. Top customers by total spend ===");
        var topCustomers = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .GroupBy(o => o.Customer.Name)
            .Select(g => new { Customer = g.Key, Total = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(x => x.Total);

        foreach (var x in topCustomers)
            Console.WriteLine($"  {x.Customer}: {x.Total:C}");

        // 4. GroupBy: средняя цена по категориям через SelectMany (method syntax)
        Console.WriteLine("\n=== 4. Average item price per category ===");
        var avgPerCategory = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.Product.Category)
            .Select(g => new { Category = g.Key, Avg = g.Average(i => i.Product.Price) });

        foreach (var x in avgPerCategory)
            Console.WriteLine($"  {x.Category}: avg {x.Avg:C}");

        // 5. GroupJoin (left join): клиенты у которых может не быть заказов
        Console.WriteLine("\n=== 5. All customers with order count (GroupJoin / left join) ===");
        var customerOrders = customers
            .GroupJoin(orders,
                c => c.Id,
                o => o.Customer.Id,
                (c, os) => new { c.Name, c.IsVip, OrderCount = os.Count(), Total = os.Sum(o => o.TotalAmount) });

        foreach (var x in customerOrders)
            Console.WriteLine($"  {x.Name} (VIP:{x.IsVip}): {x.OrderCount} orders, {x.Total:C}");

        // 6. Mixed syntax: любимая категория каждого клиента
        // Внешний запрос — query syntax для читаемости, внутренняя агрегация — method syntax
        Console.WriteLine("\n=== 6. Favourite category per customer (mixed syntax) ===");
        var favCategory =
            from o in orders
            where o.Status != OrderStatus.Cancelled
            group o by o.Customer.Name into g
            let favourite = g
                .SelectMany(o => o.Items)
                .GroupBy(i => i.Product.Category)
                .OrderByDescending(cat => cat.Count())
                .Select(cat => cat.Key)
                .FirstOrDefault() ?? "N/A"
            select new { Customer = g.Key, Favourite = favourite };

        foreach (var x in favCategory)
            Console.WriteLine($"  {x.Customer}: {x.Favourite}");
    }
}

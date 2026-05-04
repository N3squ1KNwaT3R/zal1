using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using SysConsole = System.Console;

namespace OrderFlow.Console.Services;

public static class DbLinqQueries
{
    public static async Task RunAllAsync(OrderFlowContext db)
    {
        SysConsole.WriteLine("\n=== DB-LINQ 1. VIP orders above 500 zł ===");
        decimal threshold = 500m;

        var vipOrders = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.Customer.IsVip)
            .Select(o => new
            {
                o.Id,
                o.Customer.FullName,
                o.Customer.IsVip,
                Total = o.Items.Sum(i => i.UnitPrice * i.Quantity),
                o.Status
            })
            .Where(x => x.Total > threshold)
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        foreach (var x in vipOrders)
            SysConsole.WriteLine($"  #{x.Id,-3} {x.FullName,-20} VIP={x.IsVip}  total={x.Total:C}  [{x.Status}]");

        if (vipOrders.Count == 0)
            SysConsole.WriteLine("  (brak wyników)");
        SysConsole.WriteLine("\n=== DB-LINQ 2. Ranking klientów wg sumy zamówień ===");

        var ranking = await db.Orders
            .GroupBy(o => new { o.CustomerId, o.Customer.FullName, o.Customer.IsVip })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.FullName,
                g.Key.IsVip,
                TotalSpent = g.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity)),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToListAsync();

        int rank = 1;
        foreach (var x in ranking)
            SysConsole.WriteLine($"  {rank++,2}. {x.FullName,-20} VIP={x.IsVip}  orders={x.OrderCount}  total={x.TotalSpent:C}");
        SysConsole.WriteLine("\n=== DB-LINQ 3. Średnia wartość zamówienia wg miasta ===");

        var byCity = await db.Orders
            .GroupBy(o => o.Customer.City)
            .Select(g => new
            {
                City = g.Key,
                AvgValue = g.Average(o => o.Items.Sum(i => i.UnitPrice * i.Quantity)),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.AvgValue)
            .ToListAsync();

        foreach (var x in byCity)
            SysConsole.WriteLine($"  {x.City,-12}  orders={x.OrderCount}  avg={x.AvgValue:C}");
        SysConsole.WriteLine("\n=== DB-LINQ 4. Produkty nigdy nie zamówione (anti-join) ===");

        var neverOrdered = await db.Products
            .Where(p => !p.OrderItems.Any())
            .Select(p => new { p.Id, p.Name, p.Category, p.Price })
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (neverOrdered.Count == 0)
            SysConsole.WriteLine("  (wszystkie produkty były zamówione)");
        else
            foreach (var p in neverOrdered)
                SysConsole.WriteLine($"  #{p.Id} {p.Name,-20} [{p.Category}]  {p.Price:C}");
        SysConsole.WriteLine("\n=== DB-LINQ 5. Zapytanie dynamiczne (status=New, min=0 zł) ===");

        OrderStatus? statusFilter = OrderStatus.New;
        decimal minAmount = 0m;

        IQueryable<Order> query = db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items);

        if (statusFilter.HasValue)
            query = query.Where(o => o.Status == statusFilter.Value);
        var dynamic = await query
            .Select(o => new
            {
                o.Id,
                o.Customer.FullName,
                o.Status,
                Total = o.Items.Sum(i => i.UnitPrice * i.Quantity)
            })
            .Where(x => x.Total >= minAmount)
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        SysConsole.WriteLine($"  Filtr: status={statusFilter?.ToString() ?? "brak"}, minAmount={minAmount:C}");
        foreach (var x in dynamic)
            SysConsole.WriteLine($"  #{x.Id,-3} {x.FullName,-20} [{x.Status,-12}]  total={x.Total:C}");

        if (dynamic.Count == 0)
            SysConsole.WriteLine("  (brak wyników)");
    }
}

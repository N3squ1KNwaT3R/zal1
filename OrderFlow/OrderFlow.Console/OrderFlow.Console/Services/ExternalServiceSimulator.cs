using System.Diagnostics;
using OrderFlow.Console.Models;
using SysConsole = System.Console;

namespace OrderFlow.Console.Services;

public class ExternalServiceSimulator
{
    private readonly Random _rng = new();

    public async Task<bool> CheckInventoryAsync(Product product)
    {
        var delay = _rng.Next(500, 1501);
        await Task.Delay(delay);
        var inStock = product.Price < 4000m; // droższe niż 4000 — brak
        return inStock;
    }

    public async Task<bool> ValidatePaymentAsync(Order order)
    {
        var delay = _rng.Next(1000, 2001);
        await Task.Delay(delay);
        return order.TotalAmount > 0;
    }

    public async Task<decimal> CalculateShippingAsync(Order order)
    {
        var delay = _rng.Next(300, 801);
        await Task.Delay(delay);
        return order.TotalAmount > 1000m ? 0m : 29.99m;
    }

    // Wszystkie trzy serwisy równolegle + Stopwatch
    public async Task ProcessOrderAsync(Order order)
    {
        SysConsole.WriteLine($"  [Order #{order.Id}] Starting parallel service calls...");
        var sw = Stopwatch.StartNew();

        var products = order.Items.Select(i => i.Product).Distinct().ToList();

        var inventoryTasks = products.Select(p => CheckInventoryAsync(p)).ToList();
        var paymentTask    = ValidatePaymentAsync(order);
        var shippingTask   = CalculateShippingAsync(order);

        await Task.WhenAll([.. inventoryTasks, paymentTask, shippingTask]);

        sw.Stop();

        var allInStock     = inventoryTasks.All(t => t.Result);
        var paymentOk      = paymentTask.Result;
        var shipping       = shippingTask.Result;

        SysConsole.WriteLine($"  [Order #{order.Id}] Done in {sw.ElapsedMilliseconds} ms | " +
                          $"Stock: {allInStock} | Payment: {paymentOk} | Shipping: {shipping:C}");
    }

    // Wiele zamówień równolegle z ograniczeniem do 3
    public async Task ProcessMultipleOrdersAsync(List<Order> orders)
    {
        var semaphore = new SemaphoreSlim(3);
        int done = 0;
        int total = orders.Count;

        var tasks = orders.Select(async order =>
        {
            await semaphore.WaitAsync();
            try
            {
                await ProcessOrderAsync(order);
                int current = Interlocked.Increment(ref done);
                SysConsole.WriteLine($"  >>> Przetworzono {current}/{total} zamówień");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}

using System.Collections.Concurrent;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderStatisticsUnsafe
{
    public int TotalProcessed;
    public decimal TotalRevenue;
    public Dictionary<OrderStatus, int> OrdersPerStatus = new();
    public List<string> ProcessingErrors = new();

    public void Record(Order order, bool isValid)
    {
        TotalProcessed++;
        TotalRevenue += order.TotalAmount;

        if (!OrdersPerStatus.ContainsKey(order.Status))
            OrdersPerStatus[order.Status] = 0;
        OrdersPerStatus[order.Status]++;

        if (!isValid)
            ProcessingErrors.Add($"Order #{order.Id} failed validation");
    }
}


public class OrderStatistics
{
    
    private int _totalProcessed;
    public int TotalProcessed => _totalProcessed;

    
    private decimal _totalRevenue;
    private readonly object _revenueLock = new();
    public decimal TotalRevenue => _totalRevenue;

    
    public ConcurrentDictionary<OrderStatus, int> OrdersPerStatus = new();

    
    private readonly List<string> _processingErrors = new();
    private readonly object _errorsLock = new();
    public IReadOnlyList<string> ProcessingErrors => _processingErrors;

    public void Record(Order order, bool isValid)
    {
        Interlocked.Increment(ref _totalProcessed);

        lock (_revenueLock)
            _totalRevenue += order.TotalAmount;

        OrdersPerStatus.AddOrUpdate(order.Status, 1, (_, old) => old + 1);

        if (!isValid)
            lock (_errorsLock)
                _processingErrors.Add($"Order #{order.Id} failed validation");
    }
}

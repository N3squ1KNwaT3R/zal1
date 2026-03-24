using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderProcessor
{
    private readonly List<Order> _orders;

    public OrderProcessor(List<Order> orders) => _orders = orders;

    // Predicate<Order> — фильтрация
    public List<Order> Filter(Predicate<Order> predicate) =>
        _orders.FindAll(predicate);

    // Action<Order> — действие над каждым
    public void ForEach(Action<Order> action) =>
        _orders.ForEach(action);

    // Func<Order, T> — проекция
    public List<T> Project<T>(Func<Order, T> selector) =>
        _orders.Select(selector).ToList();

    // Агрегация
    public decimal Aggregate(Func<IEnumerable<Order>, decimal> aggregator) =>
        aggregator(_orders);

    // Цепочка: filter → sort → top N → print
    public void Chain(Predicate<Order> filter,
                      Func<Order, decimal> sortKey,
                      int topN,
                      Action<Order> print)
    {
        _orders
            .FindAll(filter)
            .OrderByDescending(sortKey)
            .Take(topN)
            .ToList()
            .ForEach(print);
    }
}


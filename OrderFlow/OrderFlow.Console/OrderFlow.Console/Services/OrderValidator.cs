using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public delegate bool ValidationRule(Order order, out string errorMessage);

public class OrderValidator
{
    private readonly List<ValidationRule> _namedRules = new();
    private readonly List<(Func<Order, bool> Rule, string ErrorMessage)> _funcRules = new();

    public OrderValidator()
    {
        // Named methods
        _namedRules.Add(HasItems);
        _namedRules.Add(AmountWithinLimit);
        _namedRules.Add(AllQuantitiesPositive);

        // Lambdy
        _funcRules.Add((o => o.CreatedAt <= DateTime.Now,       "Order date is in the future"));
        _funcRules.Add((o => o.Status != OrderStatus.Cancelled, "Order is already cancelled"));
    }

    // Named rule 1
    private static bool HasItems(Order order, out string errorMessage)
    {
        errorMessage = "Order has no items";
        return order.Items.Count > 0;
    }

    // Named rule 2
    private static bool AmountWithinLimit(Order order, out string errorMessage)
    {
        errorMessage = $"Order total {order.TotalAmount} exceeds limit of 10000";
        return order.TotalAmount <= 10000m;
    }

    // Named rule 3
    private static bool AllQuantitiesPositive(Order order, out string errorMessage)
    {
        errorMessage = "All item quantities must be > 0";
        return order.Items.All(i => i.Quantity > 0);
    }

    public (bool IsValid, List<string> Errors) ValidateAll(Order order)
    {
        var errors = new List<string>();

        foreach (var rule in _namedRules)
            if (!rule(order, out var msg))
                errors.Add(msg);

        foreach (var (rule, msg) in _funcRules)
            if (!rule(order))
                errors.Add(msg);

        return (errors.Count == 0, errors);
    }
}
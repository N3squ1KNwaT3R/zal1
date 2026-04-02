using OrderFlow.Console.Events;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderPipeline
{
    private readonly OrderValidator _validator = new();

    public event EventHandler<OrderStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<OrderValidationEventArgs>? ValidationCompleted;

    private void ChangeStatus(Order order, OrderStatus newStatus)
    {
        var oldStatus = order.Status;
        order.Status = newStatus;
        StatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(order, oldStatus, newStatus));
    }

    public void ProcessOrder(Order order)
    {
        // New → Validated
        var (isValid, errors) = _validator.ValidateAll(order);
        ValidationCompleted?.Invoke(this, new OrderValidationEventArgs(order, isValid, errors));

        if (!isValid)
        {
            ChangeStatus(order, OrderStatus.Cancelled);
            return;
        }

        ChangeStatus(order, OrderStatus.Validated);

        // Validated → Processing
        ChangeStatus(order, OrderStatus.Processing);

        // Processing → Completed
        ChangeStatus(order, OrderStatus.Completed);
    }
}

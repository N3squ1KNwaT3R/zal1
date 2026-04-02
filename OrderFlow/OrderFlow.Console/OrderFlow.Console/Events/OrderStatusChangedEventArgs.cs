using OrderFlow.Console.Models;

namespace OrderFlow.Console.Events;

public class OrderStatusChangedEventArgs : EventArgs
{
    public Order Order { get; }
    public OrderStatus OldStatus { get; }
    public OrderStatus NewStatus { get; }
    public DateTime Timestamp { get; }

    public OrderStatusChangedEventArgs(Order order, OrderStatus oldStatus, OrderStatus newStatus)
    {
        Order = order;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Timestamp = DateTime.Now;
    }
}

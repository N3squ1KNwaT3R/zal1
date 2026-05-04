using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;

namespace OrderFlow.Console.Services;

public static class OrderTransactionService
{
    public static async Task ProcessOrderAsync(OrderFlowContext db, int orderId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var order = await db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new ArgumentException($"Order #{orderId} not found.");

        if (order.Status != OrderStatus.New)
            throw new InvalidOperationException(
                $"Order #{orderId} cannot be processed — current status: {order.Status}.");
        order.Status = OrderStatus.Processing;
        await db.SaveChangesAsync();
        foreach (var item in order.Items)
        {
            if (item.Product.Stock < item.Quantity)
            {
                await tx.RollbackAsync();
                throw new InsufficientStockException(item.Product.Name, item.Product.Stock, item.Quantity);
            }

            item.Product.Stock -= item.Quantity;
        }
        order.Status = OrderStatus.Completed;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}

public sealed class InsufficientStockException(string productName, int available, int required)
    : Exception($"Niewystarczający stan magazynowy dla '{productName}': dostępne={available}, wymagane={required}")
{
    public string ProductName { get; } = productName;
    public int Available { get; } = available;
    public int Required { get; } = required;
}

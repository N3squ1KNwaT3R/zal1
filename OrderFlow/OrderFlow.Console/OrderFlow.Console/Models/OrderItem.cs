namespace OrderFlow.Console.Models;

public class OrderItem
{
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    
    // Считаем цену за позицию (цена товара * количество)
    public decimal TotalPrice => Product.Price * Quantity;
}
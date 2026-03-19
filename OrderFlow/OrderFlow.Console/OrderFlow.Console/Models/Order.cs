using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderFlow.Console.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    
    // Считаем общую сумму всех позиций
    public decimal TotalAmount => Items.Sum(item => item.TotalPrice);
}
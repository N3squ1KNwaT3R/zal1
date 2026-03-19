using System;
using System.Linq;
using OrderFlow.Console.Data; // Путь к твоим данным

// 1. Инициализируем данные
SampleData.InitializeData();

Console.WriteLine("=== Проверка системы заказов ===");

// 2. Выведем список всех клиентов и их заказов
foreach (var customer in SampleData.Customers)
{
    string vipStatus = customer.IsVip ? "[VIP]" : "[Standard]";
    Console.WriteLine($"{vipStatus} Клиент: {customer.FullName}");
    
    foreach (var order in customer.Orders)
    {
        Console.WriteLine($"  - Заказ №{order.Id} от {order.OrderDate.ToShortDateString()}");
        Console.WriteLine($"    Статус: {order.Status}");
        Console.WriteLine($"    Сумма: {order.TotalAmount} PLN");
    }
    Console.WriteLine("---------------------------");
}

// 3. Пример LINQ запроса (например, только VIP заказы)
var vipTotal = SampleData.Customers
    .Where(c => c.IsVip)
    .SelectMany(c => c.Orders)
    .Sum(o => o.TotalAmount);

Console.WriteLine($"Общая сумма всех заказов VIP-клиентов: {vipTotal} PLN");
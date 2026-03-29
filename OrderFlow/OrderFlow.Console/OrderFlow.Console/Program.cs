using OrderFlow.Console.Data;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

var orders   = SampleData.Orders;
var customers = SampleData.Customers;

// ── Задача 2: Валидация ──────────────────────────────────────────
Console.WriteLine("=== TASK 2: Validation ===");
var validator = new OrderValidator();

var goodOrder = orders[0]; // Completed order с items
var (ok1, err1) = validator.ValidateAll(goodOrder);
Console.WriteLine($"\nOrder #{goodOrder.Id} valid: {ok1}");

var badOrder = orders[5]; // пустой, Cancelled
var (ok2, err2) = validator.ValidateAll(badOrder);
Console.WriteLine($"\nOrder #{badOrder.Id} valid: {ok2}");
foreach (var e in err2) Console.WriteLine($"  ✗ {e}");

// ── Задача 3: Action / Func / Predicate ─────────────────────────
Console.WriteLine("\n=== TASK 3: Processors ===");
var processor = new OrderProcessor(orders);

// Predicate — 3 разных
var completed  = processor.Filter(o => o.Status == OrderStatus.Completed);
var expensive  = processor.Filter(o => o.TotalAmount > 500);
var vipOrders  = processor.Filter(o => o.Customer.IsVip);

Console.WriteLine($"\nCompleted orders: {completed.Count}");
Console.WriteLine($"Expensive (>500): {expensive.Count}");
Console.WriteLine($"VIP customer orders: {vipOrders.Count}");

// Action — 2 применения
Console.WriteLine("\nAll orders status:");
processor.ForEach(o => Console.WriteLine($"  #{o.Id} {o.Status} {o.TotalAmount:C}"));

// Action — смена статуса New → Validated
processor.Filter(o => o.Status == OrderStatus.New)
         .ForEach(o => o.Status = OrderStatus.Validated);
Console.WriteLine("\nAfter validation (New→Validated):");
processor.ForEach(o => Console.WriteLine($"  #{o.Id} {o.Status}"));

// Func — проекция на анонимный тип
var projections = processor.Project(o => new { o.Id, o.Customer.Name, o.TotalAmount });
Console.WriteLine("\nProjection:");
projections.ForEach(x => Console.WriteLine($"  #{x.Id} {x.Name} {x.TotalAmount:C}"));

// Агрегация × 3
Console.WriteLine($"\nSum:  {processor.Aggregate(os => os.Sum(o => o.TotalAmount)):C}");
Console.WriteLine($"Avg:  {processor.Aggregate(os => os.Average(o => o.TotalAmount)):C}");
Console.WriteLine($"Max:  {processor.Aggregate(os => os.Max(o => o.TotalAmount)):C}");

// Цепочка filter → sort → top 3 → print
Console.WriteLine("\nTop 3 VIP orders by amount:");
processor.Chain(
    filter:  o => o.Customer.IsVip,
    sortKey: o => o.TotalAmount,
    topN:    3,
    print:   o => Console.WriteLine($"  #{o.Id} {o.Customer.Name} {o.TotalAmount:C}")
);

// ── Задача 4: LINQ ───────────────────────────────────────────────
Console.WriteLine("\n=== TASK 4: LINQ ===");
LinqQueries.RunAll(orders, customers);
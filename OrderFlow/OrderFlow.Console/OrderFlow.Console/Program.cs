using OrderFlow.Console.Data;
using OrderFlow.Console.Events;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

var orders   = SampleData.Orders;
var customers = SampleData.Customers;

// ── Задача 1: Zdarzenia w procesie zamówienia ────────────────────
Console.WriteLine("=== TASK 1: Order Pipeline Events ===\n");

var pipeline = new OrderPipeline();

// Subskrybent 1 — Logger konsolowy
pipeline.StatusChanged += (_, e) =>
    Console.WriteLine($"[LOG]   {e.Timestamp:HH:mm:ss} | Order #{e.Order.Id} " +
                      $"{e.OldStatus} → {e.NewStatus}");

// Subskrybent 2 — Symulacja powiadomienia e-mail
pipeline.StatusChanged += (_, e) =>
{
    if (e.NewStatus is OrderStatus.Completed or OrderStatus.Cancelled)
        Console.WriteLine($"[EMAIL] Sending to {e.Order.Customer.Name}: " +
                          $"Order #{e.Order.Id} is now {e.NewStatus}.");
};

// Subskrybent 3 — Aktualizacja statystyk
int completedCount = 0;
int cancelledCount = 0;
pipeline.StatusChanged += (_, e) =>
{
    if (e.NewStatus == OrderStatus.Completed) completedCount++;
    if (e.NewStatus == OrderStatus.Cancelled) cancelledCount++;
};

// Subskrybent 4 — Walidacja (ValidationCompleted)
pipeline.ValidationCompleted += (_, e) =>
{
    if (e.IsValid)
        Console.WriteLine($"[VALID] Order #{e.Order.Id} passed validation.");
    else
    {
        Console.WriteLine($"[VALID] Order #{e.Order.Id} FAILED validation:");
        foreach (var err in e.Errors)
            Console.WriteLine($"         ✗ {err}");
    }
};

// Przetworzenie 3 zamówień: dwa poprawne i jedno z błędem
var pipelineOrders = new List<Order>
{
    new Order { Id = 101, Customer = customers[0], Status = OrderStatus.New, CreatedAt = DateTime.Now,
        Items = [ new OrderItem { Product = SampleData.Products[0], Quantity = 1 } ] },
    new Order { Id = 102, Customer = customers[1], Status = OrderStatus.New, CreatedAt = DateTime.Now,
        Items = [ new OrderItem { Product = SampleData.Products[2], Quantity = 2 } ] },
    new Order { Id = 103, Customer = customers[2], Status = OrderStatus.New, CreatedAt = DateTime.Now,
        Items = [] },   // puste — nie przejdzie walidacji
};

foreach (var order in pipelineOrders)
{
    Console.WriteLine($"\n--- Processing Order #{order.Id} ({order.Customer.Name}) ---");
    pipeline.ProcessOrder(order);
}

Console.WriteLine($"\n[STATS] Completed: {completedCount} | Cancelled: {cancelledCount}\n");

// ── Zadanie 2: Asynchroniczne pobieranie danych ──────────────────
Console.WriteLine("=== TASK 2: Async External Services ===\n");

var simulator = new ExternalServiceSimulator();

// Zamówienia do testu (tylko te z itemami)
var asyncOrders = SampleData.Orders.Where(o => o.Items.Count > 0).ToList();

// --- Sekwencyjnie ---
Console.WriteLine("-- Sequential processing --");
var swSeq = System.Diagnostics.Stopwatch.StartNew();
foreach (var o in asyncOrders)
    await simulator.ProcessOrderAsync(o);
swSeq.Stop();
Console.WriteLine($"Sequential total: {swSeq.ElapsedMilliseconds} ms\n");

// --- Równolegle (max 3 jednocześnie) ---
Console.WriteLine("-- Parallel processing (max 3 concurrent) --");
var swPar = System.Diagnostics.Stopwatch.StartNew();
await simulator.ProcessMultipleOrdersAsync(asyncOrders);
swPar.Stop();
Console.WriteLine($"Parallel total:   {swPar.ElapsedMilliseconds} ms\n");

Console.WriteLine($"Speedup: {swSeq.ElapsedMilliseconds / (double)swPar.ElapsedMilliseconds:F2}x faster\n");

// ── Задача 2 (stara): Валидация ──────────────────────────────────
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
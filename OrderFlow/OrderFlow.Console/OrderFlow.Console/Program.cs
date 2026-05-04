using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Data;
using OrderFlow.Console.Events;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using OrderFlow.Console.Services;

var orders   = SampleData.Orders;
var customers = SampleData.Customers;
Console.WriteLine("=== EF CORE: Database Setup ===");
await using var db = new OrderFlowContext();
await db.Database.MigrateAsync();
await DatabaseSeeder.SeedAsync(db);
Console.WriteLine("Baza danych gotowa.\n");

Console.WriteLine("=== TASK 1: Order Pipeline Events ===\n");

var pipeline = new OrderPipeline();


pipeline.StatusChanged += (_, e) =>
    Console.WriteLine($"[LOG]   {e.Timestamp:HH:mm:ss} | Order #{e.Order.Id} " +
                      $"{e.OldStatus} → {e.NewStatus}");


pipeline.StatusChanged += (_, e) =>
{
    if (e.NewStatus is OrderStatus.Completed or OrderStatus.Cancelled)
        Console.WriteLine($"[EMAIL] Sending to {e.Order.Customer.FullName}: " +
                          $"Order #{e.Order.Id} is now {e.NewStatus}.");
};


int completedCount = 0;
int cancelledCount = 0;
pipeline.StatusChanged += (_, e) =>
{
    if (e.NewStatus == OrderStatus.Completed) completedCount++;
    if (e.NewStatus == OrderStatus.Cancelled) cancelledCount++;
};


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


var pipelineOrders = new List<Order>
{
    new Order { Id = 101, Customer = customers[0], Status = OrderStatus.New, CreatedAt = DateTime.Now,
        Items = [ new OrderItem { Product = SampleData.Products[0], Quantity = 1 } ] },
    new Order { Id = 102, Customer = customers[1], Status = OrderStatus.New, CreatedAt = DateTime.Now,
        Items = [ new OrderItem { Product = SampleData.Products[2], Quantity = 2 } ] },
    new Order { Id = 103, Customer = customers[2], Status = OrderStatus.New, CreatedAt = DateTime.Now,
        Items = [] },   
};

foreach (var order in pipelineOrders)
{
    Console.WriteLine($"\n--- Processing Order #{order.Id} ({order.Customer.FullName}) ---");
    pipeline.ProcessOrder(order);
}

Console.WriteLine($"\n[STATS] Completed: {completedCount} | Cancelled: {cancelledCount}\n");


Console.WriteLine("=== TASK 2: Async External Services ===\n");

var simulator = new ExternalServiceSimulator();


var asyncOrders = SampleData.Orders.Where(o => o.Items.Count > 0).ToList();


Console.WriteLine("-- Sequential processing --");
var swSeq = System.Diagnostics.Stopwatch.StartNew();
foreach (var o in asyncOrders)
    await simulator.ProcessOrderAsync(o);
swSeq.Stop();
Console.WriteLine($"Sequential total: {swSeq.ElapsedMilliseconds} ms\n");


Console.WriteLine("-- Parallel processing (max 3 concurrent) --");
var swPar = System.Diagnostics.Stopwatch.StartNew();
await simulator.ProcessMultipleOrdersAsync(asyncOrders);
swPar.Stop();
Console.WriteLine($"Parallel total:   {swPar.ElapsedMilliseconds} ms\n");

Console.WriteLine($"Speedup: {swSeq.ElapsedMilliseconds / (double)swPar.ElapsedMilliseconds:F2}x faster\n");


Console.WriteLine("=== TASK 3: Thread Safety ===\n");

var validatorTs = new OrderValidator();

var manyOrders = Enumerable.Range(1, 200)
    .Select(i => new Order
    {
        Id = i,
        Customer = customers[i % customers.Count],
        Status = (OrderStatus)(i % 4),
        CreatedAt = DateTime.Now,
        Items = i % 7 == 0
            ? []   
            : [new OrderItem { Product = SampleData.Products[i % SampleData.Products.Count], Quantity = (i % 3) + 1 }]
    })
    .ToList();


Console.WriteLine("-- UNSAFE (no synchronization) — run 3 times --");
for (int run = 1; run <= 3; run++)
{
    var unsafeStats = new OrderStatisticsUnsafe();
    try
    {
        Parallel.ForEach(manyOrders, order =>
        {
            var (valid, _) = validatorTs.ValidateAll(order);
            unsafeStats.Record(order, valid);
        });
        Console.WriteLine($"  Run {run}: Processed={unsafeStats.TotalProcessed,3} " +
                          $"Revenue={unsafeStats.TotalRevenue,10:F2} " +
                          $"Errors={unsafeStats.ProcessingErrors.Count}  ← wrong data");
    }
    catch (AggregateException ex)
    {
        Console.WriteLine($"  Run {run}: CRASH — {ex.InnerExceptions[0].GetType().Name}: " +
                          $"{ex.InnerExceptions[0].Message[..60]}...");
    }
}


Console.WriteLine("\n-- SAFE (lock + Interlocked + ConcurrentDictionary) — run 3 times --");
for (int run = 1; run <= 3; run++)
{
    var safeStats = new OrderStatistics();
    Parallel.ForEach(manyOrders, order =>
    {
        var (valid, _) = validatorTs.ValidateAll(order);
        safeStats.Record(order, valid);
    });
    Console.WriteLine($"  Run {run}: Processed={safeStats.TotalProcessed,3} " +
                      $"Revenue={safeStats.TotalRevenue,10:F2} " +
                      $"Errors={safeStats.ProcessingErrors.Count}");
}


var finalStats = new OrderStatistics();
Parallel.ForEach(manyOrders, order =>
{
    var (valid, _) = validatorTs.ValidateAll(order);
    finalStats.Record(order, valid);
});
Console.WriteLine("\n-- Final safe stats breakdown --");
Console.WriteLine($"  TotalProcessed : {finalStats.TotalProcessed}");
Console.WriteLine($"  TotalRevenue   : {finalStats.TotalRevenue:C}");
Console.WriteLine("  OrdersPerStatus:");
foreach (var kv in finalStats.OrdersPerStatus.OrderBy(k => k.Key))
    Console.WriteLine($"    {kv.Key,-12}: {kv.Value}");
Console.WriteLine($"  Errors         : {finalStats.ProcessingErrors.Count}");
Console.WriteLine();


Console.WriteLine("=== LAB3 TASK 1: OrderRepository (JSON & XML) ===\n");

var repo = new OrderRepository();
var sourceOrders = SampleData.Orders.Where(o => o.Items.Count > 0).ToList();

var jsonPath = Path.Combine("data", "orders.json");
var xmlPath  = Path.Combine("data", "orders.xml");


await repo.SaveToJsonAsync(sourceOrders, jsonPath);
await repo.SaveToXmlAsync(sourceOrders, xmlPath);
Console.WriteLine($"Saved {sourceOrders.Count} orders → {jsonPath}");
Console.WriteLine($"Saved {sourceOrders.Count} orders → {xmlPath}");


var fromJson = await repo.LoadFromJsonAsync(jsonPath);
var fromXml  = await repo.LoadFromXmlAsync(xmlPath);

Console.WriteLine($"\n-- Round-trip JSON --");
Console.WriteLine($"  Count : {fromJson.Count} (expected {sourceOrders.Count})");
Console.WriteLine($"  Revenue: {fromJson.Sum(o => o.TotalAmount):C} " +
                  $"(expected {sourceOrders.Sum(o => o.TotalAmount):C})");
foreach (var o in fromJson)
    Console.WriteLine($"  #{o.Id,-3} {o.Customer.FullName,-20} {o.TotalAmount,10:C}  status={o.Status}");

Console.WriteLine($"\n-- Round-trip XML --");
Console.WriteLine($"  Count : {fromXml.Count} (expected {sourceOrders.Count})");
Console.WriteLine($"  Revenue: {fromXml.Sum(o => o.TotalAmount):C} " +
                  $"(expected {sourceOrders.Sum(o => o.TotalAmount):C})");
foreach (var o in fromXml)
    Console.WriteLine($"  #{o.Id,-3} {o.Customer.FullName,-20} {o.TotalAmount,10:C}  status={o.Status}");


var missing = await repo.LoadFromJsonAsync("data/nonexistent.json");
Console.WriteLine($"\n  Missing file → empty list: {missing.Count == 0}");
Console.WriteLine();


Console.WriteLine("=== LAB3 TASK 2: XmlReportBuilder ===\n");

var reportBuilder = new XmlReportBuilder();
var reportOrders  = SampleData.Orders;
var reportPath    = Path.Combine("data", "report.xml");

var report = reportBuilder.BuildReport(reportOrders);
await reportBuilder.SaveReportAsync(report, reportPath);
Console.WriteLine($"Report saved → {reportPath}");


var summary = report.Root!.Element("summary")!;
Console.WriteLine($"  totalOrders  = {summary.Attribute("totalOrders")!.Value}");
Console.WriteLine($"  totalRevenue = {summary.Attribute("totalRevenue")!.Value}");

Console.WriteLine("\nBy status:");
foreach (var el in report.Root.Element("byStatus")!.Elements("status"))
    Console.WriteLine($"  {el.Attribute("name")!.Value,-12} " +
                      $"count={el.Attribute("count")!.Value,2}  " +
                      $"revenue={el.Attribute("revenue")!.Value}");

Console.WriteLine("\nBy customer:");
foreach (var el in report.Root.Element("byCustomer")!.Elements("customer"))
    Console.WriteLine($"  [{el.Attribute("id")!.Value}] {el.Attribute("name")!.Value,-20} " +
                      $"isVip={el.Attribute("isVip")!.Value,-5}  " +
                      $"spent={el.Element("totalSpent")!.Value}");


var highValueIds = await reportBuilder.FindHighValueOrderIdsAsync(reportPath, 1000m);
Console.WriteLine($"\nOrders with total > 1000 zł (read from file):");
Console.WriteLine($"  Ids: [{string.Join(", ", highValueIds)}]");
Console.WriteLine();


Console.WriteLine("=== TASK 2: Validation ===");
var validator = new OrderValidator();

var goodOrder = orders[0]; 
var (ok1, err1) = validator.ValidateAll(goodOrder);
Console.WriteLine($"\nOrder #{goodOrder.Id} valid: {ok1}");

var badOrder = orders[5]; 
var (ok2, err2) = validator.ValidateAll(badOrder);
Console.WriteLine($"\nOrder #{badOrder.Id} valid: {ok2}");
foreach (var e in err2) Console.WriteLine($"  ✗ {e}");


Console.WriteLine("\n=== TASK 3: Processors ===");
var processor = new OrderProcessor(orders);


var completed  = processor.Filter(o => o.Status == OrderStatus.Completed);
var expensive  = processor.Filter(o => o.TotalAmount > 500);
var vipOrders  = processor.Filter(o => o.Customer.IsVip);

Console.WriteLine($"\nCompleted orders: {completed.Count}");
Console.WriteLine($"Expensive (>500): {expensive.Count}");
Console.WriteLine($"VIP customer orders: {vipOrders.Count}");


Console.WriteLine("\nAll orders status:");
processor.ForEach(o => Console.WriteLine($"  #{o.Id} {o.Status} {o.TotalAmount:C}"));


processor.Filter(o => o.Status == OrderStatus.New)
         .ForEach(o => o.Status = OrderStatus.Validated);
Console.WriteLine("\nAfter validation (New→Validated):");
processor.ForEach(o => Console.WriteLine($"  #{o.Id} {o.Status}"));


var projections = processor.Project(o => new { o.Id, o.Customer.FullName, o.TotalAmount });
Console.WriteLine("\nProjection:");
projections.ForEach(x => Console.WriteLine($"  #{x.Id} {x.FullName} {x.TotalAmount:C}"));


Console.WriteLine($"\nSum:  {processor.Aggregate(os => os.Sum(o => o.TotalAmount)):C}");
Console.WriteLine($"Avg:  {processor.Aggregate(os => os.Average(o => o.TotalAmount)):C}");
Console.WriteLine($"Max:  {processor.Aggregate(os => os.Max(o => o.TotalAmount)):C}");


Console.WriteLine("\nTop 3 VIP orders by amount:");
processor.Chain(
    filter:  o => o.Customer.IsVip,
    sortKey: o => o.TotalAmount,
    topN:    3,
    print:   o => Console.WriteLine($"  #{o.Id} {o.Customer.FullName} {o.TotalAmount:C}")
);


Console.WriteLine("\n=== TASK 4: LINQ ===");
LinqQueries.RunAll(orders, customers);


Console.WriteLine("\n=== LAB3 TASK 3: InboxWatcher (FileSystemWatcher) ===\n");

var inboxPath = "inbox";
var inboxPipeline = new OrderPipeline();


inboxPipeline.StatusChanged += (_, e) =>
    Console.WriteLine($"[INBOX-EVENT] Order #{e.Order.Id} ({e.Order.Customer.FullName}) " +
                      $"{e.OldStatus} → {e.NewStatus}");

inboxPipeline.ValidationCompleted += (_, e) =>
{
    if (!e.IsValid)
    {
        Console.WriteLine($"[INBOX-VALID] Order #{e.Order.Id} nie przeszło walidacji:");
        foreach (var err in e.Errors)
            Console.WriteLine($"              ✗ {err}");
    }
};

using var inboxWatcher = new InboxWatcher(inboxPath, inboxPipeline);
Console.WriteLine($"Watcher uruchomiony na katalogu '{inboxPath}/'");
Console.WriteLine("Co 3 s program automatycznie wrzuci testowy plik JSON.\n");

var inboxRepo = new OrderRepository();
int batchNo = 0;

for (int i = 0; i < 3; i++)
{
    await Task.Delay(3000);
    batchNo++;

    var testOrders = new List<Order>
    {
        new Order
        {
            Id = 2000 + batchNo * 10,
            Customer = customers[batchNo % customers.Count],
            Status = OrderStatus.New,
            CreatedAt = DateTime.Now,
            Items = [ new OrderItem { Product = SampleData.Products[batchNo % SampleData.Products.Count], Quantity = 2 } ],
        },
        new Order
        {
            Id = 2000 + batchNo * 10 + 1,
            Customer = customers[(batchNo + 1) % customers.Count],
            Status = OrderStatus.New,
            CreatedAt = DateTime.Now,
            Items = [ new OrderItem { Product = SampleData.Products[(batchNo + 1) % SampleData.Products.Count], Quantity = 1 } ],
        },
    };

    var fileName = $"batch_{batchNo:D3}_{DateTime.Now:HHmmss}.json";
    var filePath = Path.Combine(inboxPath, fileName);
    await inboxRepo.SaveToJsonAsync(testOrders, filePath);
    Console.WriteLine($"\n[DEMO] Wrzucono plik: {fileName} ({testOrders.Count} zamówienia)");
}


await Task.Delay(2000);
Console.WriteLine("\n[DEMO] Demo InboxWatcher zakończone.");
Console.WriteLine("\n=== EF CORE: CRUD ===\n");

await using var crudDb = new OrderFlowContext();
Console.WriteLine("-- CREATE --");
var firstCustomer = await crudDb.Customers.FirstAsync();
var firstTwoProducts = await crudDb.Products.Take(2).ToListAsync();

var newOrder = new Order
{
    CustomerId = firstCustomer.Id,
    Status = OrderStatus.New,
    CreatedAt = DateTime.UtcNow,
    Notes = "Zamówienie testowe CRUD",
    Items =
    [
        new() { ProductId = firstTwoProducts[0].Id, UnitPrice = firstTwoProducts[0].Price, Quantity = 1 },
        new() { ProductId = firstTwoProducts[1].Id, UnitPrice = firstTwoProducts[1].Price, Quantity = 3 },
    ]
};
crudDb.Orders.Add(newOrder);
await crudDb.SaveChangesAsync();
Console.WriteLine($"Dodano zamówienie #{newOrder.Id} z {newOrder.Items.Count} pozycjami\n");
Console.WriteLine("-- READ --");
var dbOrders = await crudDb.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items)
        .ThenInclude(i => i.Product)
    .OrderBy(o => o.Id)
    .ToListAsync();

foreach (var o in dbOrders)
{
    Console.WriteLine($"  #{o.Id,-3} [{o.Status,-12}] {o.Customer.FullName,-20}  total={o.Items.Sum(i => i.TotalPrice):C}");
    foreach (var item in o.Items)
        Console.WriteLine($"       • {item.Product.Name,-20} x{item.Quantity}  @{item.UnitPrice:C}");
}
Console.WriteLine("\n-- UPDATE --");
var orderToUpdate = await crudDb.Orders.FirstAsync(o => o.Status == OrderStatus.New);
orderToUpdate.Status = OrderStatus.Processing;
orderToUpdate.Notes  = "Przekazano do realizacji";
await crudDb.SaveChangesAsync();
Console.WriteLine($"Zaktualizowano zamówienie #{orderToUpdate.Id}: status={orderToUpdate.Status}, notes=\"{orderToUpdate.Notes}\"\n");
Console.WriteLine("-- DELETE --");
var cancelledOrder = await crudDb.Orders
    .Include(o => o.Items)
    .FirstOrDefaultAsync(o => o.Status == OrderStatus.Cancelled);
if (cancelledOrder is not null)
{
    crudDb.Orders.Remove(cancelledOrder);
    await crudDb.SaveChangesAsync();
    Console.WriteLine($"Usunięto anulowane zamówienie #{cancelledOrder.Id} (cascade: {cancelledOrder.Items.Count} pozycji)");
}
Console.WriteLine("\nRestrict — próba usunięcia klienta z zamówieniami:");
await using var restrictDb = new OrderFlowContext();
var customerToDelete = await restrictDb.Customers.FirstAsync();
restrictDb.Customers.Remove(customerToDelete);
try
{
    await restrictDb.SaveChangesAsync();
    Console.WriteLine("  Klient usunięty (nieoczekiwane!)");
}
catch (DbUpdateException ex)
{
    Console.WriteLine($"  Restrict OK — wyjątek: {ex.InnerException?.Message ?? ex.Message}");
}
Console.WriteLine("\n=== EF CORE: IQueryable LINQ ===");
await using var linqDb = new OrderFlowContext();
await DbLinqQueries.RunAllAsync(linqDb);
Console.WriteLine("\n=== EF CORE: Transakcja ProcessOrder ===\n");
Console.WriteLine("-- Scenariusz 1: zamówienie z wystarczającym stanem magazynowym --");
await using var txDb1 = new OrderFlowContext();
var successOrder = await txDb1.Orders
    .Include(o => o.Items).ThenInclude(i => i.Product)
    .FirstOrDefaultAsync(o => o.Status == OrderStatus.New && o.Items.Any());

if (successOrder is not null)
{
    foreach (var item in successOrder.Items)
        item.Product.Stock = item.Quantity + 5;
    await txDb1.SaveChangesAsync();

    try
    {
        await OrderTransactionService.ProcessOrderAsync(txDb1, successOrder.Id);
        Console.WriteLine($"  OK — zamówienie #{successOrder.Id} przetworzone → Completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  BŁĄD (nieoczekiwany): {ex.Message}");
    }
}
Console.WriteLine("\n-- Scenariusz 2: zamówienie z niewystarczającym stanem magazynowym --");
await using var txDb2 = new OrderFlowContext();
var lowStockProduct = await txDb2.Products.FirstAsync();
lowStockProduct.Stock = 0;

var failCustomer = await txDb2.Customers.FirstAsync();
var failOrder = new Order
{
    CustomerId = failCustomer.Id,
    Status = OrderStatus.New,
    CreatedAt = DateTime.UtcNow,
    Notes = "Test rollback",
    Items = [ new OrderItem { ProductId = lowStockProduct.Id, UnitPrice = lowStockProduct.Price, Quantity = 2 } ]
};
txDb2.Orders.Add(failOrder);
await txDb2.SaveChangesAsync();

try
{
    await OrderTransactionService.ProcessOrderAsync(txDb2, failOrder.Id);
    Console.WriteLine($"  BŁĄD — powinien był rzucić wyjątek!");
}
catch (InsufficientStockException ex)
{
    Console.WriteLine($"  Rollback OK — {ex.Message}");
    await txDb2.Entry(failOrder).ReloadAsync();
    Console.WriteLine($"  Status zamówienia po rollback: {failOrder.Status} (powinien być New)");
}
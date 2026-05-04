using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;
using SysConsole = System.Console;

namespace OrderFlow.Console.Persistence;

public class InboxWatcher : IDisposable
{
    private readonly OrderPipeline _pipeline;
    private readonly FileSystemWatcher _watcher;
    private readonly SemaphoreSlim _semaphore = new(2);
    private readonly HashSet<string> _inProgress = [];
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public InboxWatcher(string inboxPath, OrderPipeline pipeline)
    {
        _pipeline = pipeline;

        Directory.CreateDirectory(inboxPath);
        Directory.CreateDirectory(Path.Combine(inboxPath, "processed"));
        Directory.CreateDirectory(Path.Combine(inboxPath, "failed"));

        _watcher = new FileSystemWatcher(inboxPath, "*.json")
        {
            NotifyFilter = NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        _watcher.Created += OnFileCreated;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            if (!_inProgress.Add(e.FullPath))
                return;
        }
        _ = ProcessFileAsync(e.FullPath);
    }

    private async Task ProcessFileAsync(string filePath)
    {
        await _semaphore.WaitAsync();
        try
        {
            SysConsole.WriteLine($"[INBOX] Wykryto plik: {Path.GetFileName(filePath)}");
            List<Order>? orders = null;
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                await Task.Delay(attempt == 1 ? 300 : 200);
                try
                {
                    await using var stream = new FileStream(
                        filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    orders = await JsonSerializer.DeserializeAsync<List<Order>>(stream, JsonOptions);
                    break;
                }
                catch (IOException) when (attempt < 5)
                {
                    SysConsole.WriteLine($"[INBOX] Plik zajęty, próba {attempt}/5...");
                }
            }

            if (orders is null or { Count: 0 })
                throw new InvalidDataException("Plik nie zawiera zamówień lub nie udało się go odczytać.");

            SysConsole.WriteLine($"[INBOX] Importuję {orders.Count} zamówienie(a) z {Path.GetFileName(filePath)}");

            foreach (var order in orders)
                _pipeline.ProcessOrder(order);

            var processedPath = Path.Combine(
                Path.GetDirectoryName(filePath)!, "processed", Path.GetFileName(filePath));
            File.Move(filePath, processedPath, overwrite: true);
            SysConsole.WriteLine($"[INBOX] Przeniesiono do processed/: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            SysConsole.WriteLine($"[INBOX] BŁĄD podczas przetwarzania {Path.GetFileName(filePath)}: {ex.Message}");
            try
            {
                var failedDir = Path.Combine(Path.GetDirectoryName(filePath)!, "failed");
                var failedPath = Path.Combine(failedDir, Path.GetFileName(filePath));
                File.Move(filePath, failedPath, overwrite: true);
                await File.WriteAllTextAsync(failedPath + ".error.txt", ex.ToString());
                SysConsole.WriteLine($"[INBOX] Przeniesiono do failed/: {Path.GetFileName(filePath)}");
            }
            catch { /* best-effort */ }
        }
        finally
        {
            lock (_lock)
                _inProgress.Remove(filePath);
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _semaphore.Dispose();
    }
}

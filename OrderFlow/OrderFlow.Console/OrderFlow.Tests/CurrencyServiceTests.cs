using System.Net;
using System.Text;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

// ── Reużywalny handler bez Moq ────────────────────────────────────────────────
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public int CallCount { get; private set; }
    public Uri? LastRequestUri { get; private set; }

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    /// Skrócony konstruktor: stały JSON + kod statusu
    public TestHttpMessageHandler(string json, HttpStatusCode status = HttpStatusCode.OK)
        : this(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        })
    { }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequestUri = request.RequestUri;
        return Task.FromResult(_respond(request));
    }
}

// ── Testy CurrencyService ─────────────────────────────────────────────────────
public class CurrencyServiceTests
{
    private const string UsdJson = """
        {
          "table": "A", "currency": "dolar amerykański", "code": "USD",
          "rates": [{ "no": "094/A/NBP/2025", "effectiveDate": "2025-05-14", "mid": 4.0123 }]
        }
        """;

    private const string EurJson = """
        {
          "table": "A", "currency": "euro", "code": "EUR",
          "rates": [{ "no": "094/A/NBP/2025", "effectiveDate": "2025-05-14", "mid": 4.2543 }]
        }
        """;

    // 1. Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_ValidCurrency_ReturnsMidRate()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(UsdJson);
        var svc = new CurrencyService(new HttpClient(handler));

        // Act
        var rate = await svc.GetRateAsync("USD");

        // Assert
        Assert.Equal(4.0123m, rate);
    }

    // 2. Przypadek specjalny: PLN → 1.0, bez wywołania API ─────────────────────

    [Fact]
    public async Task GetRateAsync_PLN_ReturnsOneWithoutCallingApi()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(UsdJson); // gdyby wywołał — zwróciłby USD
        var svc = new CurrencyService(new HttpClient(handler));

        // Act
        var rate = await svc.GetRateAsync("PLN");

        // Assert
        Assert.Equal(1.0m, rate);
        Assert.Equal(0, handler.CallCount); // API NIE zostało odpytane
    }

    // 3. 404 → null ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_UnknownCurrency404_ReturnsNull()
    {
        // Arrange
        var handler = new TestHttpMessageHandler("Not Found", HttpStatusCode.NotFound);
        var svc = new CurrencyService(new HttpClient(handler));

        // Act
        var rate = await svc.GetRateAsync("XYZ");

        // Assert
        Assert.Null(rate);
    }

    // 4. 500 → CurrencyServiceException ────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_ServerError500_ThrowsCurrencyServiceException()
    {
        // Arrange
        var handler = new TestHttpMessageHandler("error", HttpStatusCode.InternalServerError);
        var svc = new CurrencyService(new HttpClient(handler));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CurrencyServiceException>(
            () => svc.GetRateAsync("USD"));

        Assert.Equal(500, ex.StatusCode);
    }

    // 5. ConvertAsync z dwóch walut ─────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_UsdToEur_ReturnsCorrectAmount()
    {
        // Arrange
        // USD = 4.0123 PLN, EUR = 4.2543 PLN
        // 100 USD → 100 * 4.0123 / 4.2543 ≈ 94.31 EUR
        var callCount = 0;
        var handler = new TestHttpMessageHandler(req =>
        {
            callCount++;
            var url = req.RequestUri!.ToString();
            var json = url.Contains("USD", StringComparison.OrdinalIgnoreCase) ? UsdJson : EurJson;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var svc = new CurrencyService(new HttpClient(handler));

        // Act
        var result = await svc.ConvertAsync(100m, "USD", "EUR");

        // Assert
        var expected = 100m * 4.0123m / 4.2543m;
        Assert.Equal(expected, result, precision: 4);
    }

    // 6. URL zawiera kod waluty ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_CallsNbpUrlWithCurrencyCode()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(UsdJson);
        var svc = new CurrencyService(new HttpClient(handler));

        // Act
        await svc.GetRateAsync("USD");

        // Assert
        Assert.NotNull(handler.LastRequestUri);
        var url = handler.LastRequestUri!.ToString();
        Assert.Contains("/exchangerates/rates/A/USD/", url, StringComparison.OrdinalIgnoreCase);
    }

    // 7. Bonus: cache — dwa wywołania → API odpytane tylko raz ──────────────────

    [Fact]
    public async Task GetRateAsync_CalledTwiceForSameCurrency_ApiCalledOnlyOnce()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(UsdJson);
        var svc = new CurrencyService(new HttpClient(handler));

        // Act
        var rate1 = await svc.GetRateAsync("USD");
        var rate2 = await svc.GetRateAsync("USD");

        // Assert
        Assert.Equal(rate1, rate2);
        Assert.Equal(1, handler.CallCount); // cache: drugie wywołanie nie trafiło do API
    }

    // 8. ConvertAsync PLN→waluta używa GetRateAsync(PLN)=1, nie wywołuje API ────

    [Fact]
    public async Task ConvertAsync_FromPln_DoesNotCallApiForPln()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(UsdJson);
        var svc = new CurrencyService(new HttpClient(handler));

        // Act
        var result = await svc.ConvertAsync(400m, "PLN", "USD"); // 400 PLN / 4.0123 ≈ 99.69 USD

        // Assert
        var expected = 400m * 1.0m / 4.0123m;
        Assert.Equal(expected, result, precision: 4);
        Assert.Equal(1, handler.CallCount); // tylko USD odpytane, PLN → bez wywołania
    }
}

using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class CurrencyServiceTests
{
    private static HttpClient MakeClient(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        return new HttpClient(handler.Object);
    }

    [Fact]
    public async Task GetRateAsync_ReturnsCorrectMid()
    {
        const string json = """
            {
              "table": "A",
              "currency": "euro",
              "code": "EUR",
              "rates": [{ "no": "094/A/NBP/2025", "effectiveDate": "2025-05-14", "mid": 4.2543 }]
            }
            """;

        var svc = new CurrencyService(MakeClient(json));
        var rate = await svc.GetRateAsync("EUR");

        Assert.Equal(4.2543m, rate);
    }

    [Fact]
    public async Task GetRateAsync_ThrowsOnHttpError()
    {
        var svc = new CurrencyService(MakeClient("", HttpStatusCode.NotFound));

        await Assert.ThrowsAsync<HttpRequestException>(() => svc.GetRateAsync("XYZ"));
    }

    [Fact]
    public async Task GetRateAsync_CallsUrlContainingCurrencyCode()
    {
        const string json = """{ "rates": [{ "mid": 3.9 }] }""";
        Uri? calledUri = null;

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => calledUri = req.RequestUri)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var svc = new CurrencyService(new HttpClient(handler.Object));
        await svc.GetRateAsync("USD");

        Assert.NotNull(calledUri);
        Assert.Contains("USD", calledUri!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRateAsync_MultipleRates_ReturnsFirst()
    {
        const string json = """
            {
              "rates": [
                { "mid": 4.10 },
                { "mid": 4.20 }
              ]
            }
            """;

        var svc = new CurrencyService(MakeClient(json));
        var rate = await svc.GetRateAsync("EUR");

        Assert.Equal(4.10m, rate);
    }
}

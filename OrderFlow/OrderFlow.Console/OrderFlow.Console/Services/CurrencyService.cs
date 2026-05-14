using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderFlow.Console.Services;

public interface ICurrencyService
{
    Task<decimal> GetRateAsync(string currencyCode);
}

public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _httpClient;

    public CurrencyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> GetRateAsync(string currencyCode)
    {
        var response = await _httpClient.GetAsync(
            $"https://api.nbp.pl/api/exchangerates/rates/a/{currencyCode}/?format=json");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<NbpRateResponse>(json)!;
        return result.Rates[0].Mid;
    }
}

file record NbpRateResponse(
    [property: JsonPropertyName("rates")] List<NbpRate> Rates);

file record NbpRate(
    [property: JsonPropertyName("mid")] decimal Mid);

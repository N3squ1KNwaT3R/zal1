using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderFlow.Console.Services;

public interface ICurrencyService
{
    /// <summary>Zwraca kurs średni do PLN lub null jeśli waluta nie istnieje (404).</summary>
    Task<decimal?> GetRateAsync(string currencyCode);

    /// <summary>Przelicza kwotę z fromCurrency na toCurrency przez PLN jako wspólny mianownik.</summary>
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
}

public class CurrencyService : ICurrencyService
{
    private readonly HttpClient _httpClient;

    // Bonus: cache w ramach sesji — klucz bez rozróżniania wielkości liter
    private readonly ConcurrentDictionary<string, decimal> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public CurrencyService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<decimal?> GetRateAsync(string currencyCode)
    {
        // Przypadek specjalny: PLN → 1, bez wywołania API
        if (currencyCode.Equals("PLN", StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        // Cache hit
        if (_cache.TryGetValue(currencyCode, out var cached))
            return cached;

        var response = await _httpClient.GetAsync(
            $"https://api.nbp.pl/api/exchangerates/rates/A/{currencyCode}/?format=json");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new CurrencyServiceException(
                (int)response.StatusCode,
                $"NBP API zwróciło {(int)response.StatusCode} dla waluty '{currencyCode}'");

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<NbpRateResponse>(json)!;
        var rate = result.Rates[0].Mid;

        _cache[currencyCode] = rate;
        return rate;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        var fromRate = await GetRateAsync(fromCurrency)
            ?? throw new CurrencyServiceException(404, $"Nieznana waluta źródłowa: '{fromCurrency}'");

        var toRate = await GetRateAsync(toCurrency)
            ?? throw new CurrencyServiceException(404, $"Nieznana waluta docelowa: '{toCurrency}'");

        // amount [fromCurrency] * fromRate [PLN/fromCurrency] / toRate [PLN/toCurrency]
        return amount * fromRate / toRate;
    }
}

file record NbpRateResponse(
    [property: JsonPropertyName("rates")] List<NbpRate> Rates);

file record NbpRate(
    [property: JsonPropertyName("mid")] decimal Mid);

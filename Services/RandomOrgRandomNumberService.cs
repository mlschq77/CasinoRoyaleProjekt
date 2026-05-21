using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CasinoRoyale.Services;

public class RandomOrgRandomNumberService : IRandomNumberService
{
    private readonly HttpClient _httpClient;
    private readonly RandomOrgOptions _options;

    public RandomOrgRandomNumberService(HttpClient httpClient, IOptions<RandomOrgOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<int> GetIntAsync(
        int minInclusive,
        int maxExclusive,
        CancellationToken cancellationToken = default)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Gorna granica musi byc wieksza od dolnej granicy.");

        var values = await GetIntsAsync(1, minInclusive, maxExclusive - 1, true, cancellationToken);
        return values[0];
    }

    public Task<IReadOnlyList<int>> GetUniqueIntsAsync(
        int count,
        int minInclusive,
        int maxInclusive,
        CancellationToken cancellationToken = default)
    {
        return GetIntsAsync(count, minInclusive, maxInclusive, false, cancellationToken);
    }

    public async Task<IReadOnlyList<int>> GetIntsAsync(
        int count,
        int minInclusive,
        int maxInclusive,
        bool allowDuplicates = true,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(count, minInclusive, maxInclusive, allowDuplicates);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Brakuje klucza API Random.org. Skonfiguruj wartosc RandomOrg:ApiKey.");

        var request = new RandomOrgRequest(
            "2.0",
            "generateIntegers",
            new RandomOrgGenerateIntegersParams(
                _options.ApiKey,
                count,
                minInclusive,
                maxInclusive,
                allowDuplicates,
                10),
            Guid.NewGuid().ToString("N"));

        using var response = await _httpClient.PostAsJsonAsync(string.Empty, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<RandomOrgResponse>(cancellationToken);

        if (body?.Error is not null)
            throw new InvalidOperationException($"Blad Random.org {body.Error.Code}: {body.Error.Message}");

        return body?.Result?.Random?.Data
            ?? throw new InvalidOperationException("Odpowiedz Random.org nie zawierala danych losowych.");
    }

    private static void ValidateRequest(int count, int minInclusive, int maxInclusive, bool allowDuplicates)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Liczba wynikow musi byc wieksza od zera.");

        if (maxInclusive < minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxInclusive), "Gorna granica musi byc wieksza lub rowna dolnej granicy.");

        var rangeSize = maxInclusive - minInclusive + 1;
        if (!allowDuplicates && count > rangeSize)
            throw new ArgumentOutOfRangeException(nameof(count), "Liczba wynikow nie moze przekraczac rozmiaru zakresu, gdy duplikaty sa wylaczone.");
    }

    private sealed record RandomOrgRequest(
        [property: JsonPropertyName("jsonrpc")] string JsonRpc,
        [property: JsonPropertyName("method")] string Method,
        [property: JsonPropertyName("params")] RandomOrgGenerateIntegersParams Params,
        [property: JsonPropertyName("id")] string Id);

    private sealed record RandomOrgGenerateIntegersParams(
        [property: JsonPropertyName("apiKey")] string ApiKey,
        [property: JsonPropertyName("n")] int Count,
        [property: JsonPropertyName("min")] int Min,
        [property: JsonPropertyName("max")] int Max,
        [property: JsonPropertyName("replacement")] bool Replacement,
        [property: JsonPropertyName("base")] int Base);

    private sealed class RandomOrgResponse
    {
        [JsonPropertyName("result")]
        public RandomOrgResult? Result { get; set; }

        [JsonPropertyName("error")]
        public RandomOrgError? Error { get; set; }
    }

    private sealed class RandomOrgResult
    {
        [JsonPropertyName("random")]
        public RandomOrgRandom? Random { get; set; }
    }

    private sealed class RandomOrgRandom
    {
        [JsonPropertyName("data")]
        public List<int>? Data { get; set; }
    }

    private sealed class RandomOrgError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}

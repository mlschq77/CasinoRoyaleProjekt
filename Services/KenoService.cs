using System.Security.Cryptography;

namespace CasinoRoyale.Services;

public class KenoService
{
    private static readonly IReadOnlyDictionary<int, decimal[]> PayTables = new Dictionary<int, decimal[]>
    {
        [1] = new[] { 0m, 3.8m },
        [2] = new[] { 0m, 1.2m, 10m },
        [3] = new[] { 0m, 0m, 2.2m, 25m },
        [4] = new[] { 0m, 0m, 1.5m, 6m, 80m },
        [5] = new[] { 0m, 0m, 1m, 3.2m, 18m, 250m },
        [6] = new[] { 0m, 0m, 0m, 2m, 8m, 70m, 600m },
        [7] = new[] { 0m, 0m, 0m, 1.5m, 5m, 30m, 250m, 1200m },
        [8] = new[] { 0m, 0m, 0m, 1m, 3.5m, 14m, 100m, 800m, 2500m },
        [9] = new[] { 0m, 0m, 0m, 0m, 2.5m, 9m, 45m, 350m, 1800m, 5000m },
        [10] = new[] { 0m, 0m, 0m, 0m, 2m, 6m, 25m, 180m, 1000m, 4000m, 10000m }
    };

    public KenoResult Play(decimal bet, IReadOnlyCollection<int> selectedNumbers)
    {
        var normalized = NormalizeNumbers(selectedNumbers);
        var drawn = DrawNumbers();
        var hits = normalized.Count(drawn.Contains);
        var multiplier = PayTables[normalized.Count][hits];
        var win = decimal.Round(bet * multiplier, 2);

        return new KenoResult(normalized, drawn, hits, multiplier, win);
    }

    public static List<int> NormalizeNumbers(IEnumerable<int> numbers)
    {
        return numbers
            .Where(number => number is >= 1 and <= 40)
            .Distinct()
            .OrderBy(number => number)
            .ToList();
    }

    public static bool IsValidSelection(IReadOnlyCollection<int> numbers)
    {
        return numbers.Count is >= 1 and <= 10;
    }

    public IReadOnlyList<decimal> GetPayTable(int picked)
    {
        return PayTables.TryGetValue(picked, out var table) ? table : PayTables[10];
    }

    private static List<int> DrawNumbers()
    {
        var pool = Enumerable.Range(1, 40).ToList();
        var drawn = new List<int>(10);

        while (drawn.Count < 10)
        {
            var index = RandomNumberGenerator.GetInt32(0, pool.Count);
            drawn.Add(pool[index]);
            pool.RemoveAt(index);
        }

        drawn.Sort();
        return drawn;
    }
}

public record KenoResult(
    IReadOnlyList<int> SelectedNumbers,
    IReadOnlyList<int> DrawnNumbers,
    int Hits,
    decimal Multiplier,
    decimal Win);

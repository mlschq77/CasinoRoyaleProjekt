using System.Security.Cryptography;

namespace CasinoRoyale.Services;

public class PlinkoService
{
    private static readonly IReadOnlyDictionary<string, decimal[]> Multipliers = new Dictionary<string, decimal[]>
    {
        ["low"] = new[] { 2.6m, 1.3m, 0.9m, 0.7m, 0.5m, 0.7m, 0.9m, 1.3m, 2.6m },
        ["medium"] = new[] { 5.6m, 2.1m, 1.1m, 0.7m, 0.4m, 0.7m, 1.1m, 2.1m, 5.6m },
        ["high"] = new[] { 13m, 3.8m, 1.4m, 0.4m, 0.2m, 0.4m, 1.4m, 3.8m, 13m }
    };

    public PlinkoResult Play(decimal bet, string risk)
    {
        var normalizedRisk = NormalizeRisk(risk);
        var multipliers = Multipliers[normalizedRisk];
        var path = GeneratePath(multipliers.Length - 1);
        var landingSlot = path.Count(step => step == PlinkoStep.Right);
        var multiplier = multipliers[landingSlot];

        return new PlinkoResult(
            normalizedRisk,
            path,
            landingSlot,
            multiplier,
            decimal.Round(bet * multiplier, 2));
    }

    public IReadOnlyList<decimal> GetMultipliers(string risk)
    {
        return Multipliers[NormalizeRisk(risk)];
    }

    private static string NormalizeRisk(string? risk)
    {
        var normalized = risk?.Trim().ToLowerInvariant();
        return Multipliers.ContainsKey(normalized ?? string.Empty) ? normalized! : "medium";
    }

    private static List<PlinkoStep> GeneratePath(int rows)
    {
        var path = new List<PlinkoStep>(rows);

        for (var row = 0; row < rows; row++)
        {
            path.Add(RandomNumberGenerator.GetInt32(0, 2) == 0 ? PlinkoStep.Left : PlinkoStep.Right);
        }

        return path;
    }
}

public enum PlinkoStep
{
    Left,
    Right
}

public record PlinkoResult(
    string Risk,
    IReadOnlyList<PlinkoStep> Path,
    int LandingSlot,
    decimal Multiplier,
    decimal Win);

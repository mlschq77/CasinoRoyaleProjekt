using System.Security.Cryptography;

namespace CasinoRoyale.Services;

public class DiceService
{
    private const decimal HouseEdge = 0.99m;

    public DiceResult Play(decimal bet, string mode, int target)
    {
        var normalizedMode = NormalizeMode(mode);
        var roll = RandomNumberGenerator.GetInt32(1, 101);
        var chance = normalizedMode == "over" ? 100 - target : target - 1;
        var multiplier = decimal.Round(100m / chance * HouseEdge, 2);
        var won = normalizedMode == "over" ? roll > target : roll < target;
        var win = won ? decimal.Round(bet * multiplier, 2) : 0m;

        return new DiceResult(normalizedMode, target, roll, chance, multiplier, win);
    }

    public static bool IsValidTarget(int target) => target is >= 2 and <= 98;

    public static string NormalizeMode(string? mode)
    {
        var normalized = mode?.Trim().ToLowerInvariant();
        return normalized == "under" ? "under" : "over";
    }
}

public record DiceResult(
    string Mode,
    int Target,
    int Roll,
    int Chance,
    decimal Multiplier,
    decimal Win);

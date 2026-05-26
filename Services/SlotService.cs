using System.Security.Cryptography;

namespace CasinoRoyale.Services;

public class SlotService
{
    private static readonly SlotSymbol[] Symbols =
    [
        new("cherry", "Wisnia", "🍒", 4.00m, 30),
        new("lemon", "Cytryna", "🍋", 5.00m, 24),
        new("plum", "Sliwka", "🍇", 7.50m, 18),
        new("watermelon", "Arbuz", "🍉", 10.00m, 14),
        new("bell", "Dzwonek", "🔔", 16.00m, 9),
        new("seven", "Siodemka", "7", 25.00m, 5)
    ];

    private static readonly int TotalWeight = Symbols.Sum(symbol => symbol.Weight);

    public SlotResult Play(decimal bet)
    {
        var reels = new[]
        {
            PickSymbol(),
            PickSymbol(),
            PickSymbol()
        };

        var multiplier = GetMultiplier(reels);
        var win = multiplier > 0 ? decimal.Round(bet * multiplier, 2) : 0m;

        return new SlotResult(reels, multiplier, win);
    }

    public IReadOnlyList<SlotSymbol> GetPayTable() => Symbols;

    private static SlotSymbol PickSymbol()
    {
        var roll = RandomNumberGenerator.GetInt32(TotalWeight);
        var cumulative = 0;

        foreach (var symbol in Symbols)
        {
            cumulative += symbol.Weight;
            if (roll < cumulative)
                return symbol;
        }

        return Symbols[0];
    }

    private static decimal GetMultiplier(IReadOnlyList<SlotSymbol> reels)
    {
        if (reels.All(symbol => symbol.Id == reels[0].Id))
            return reels[0].TripleMultiplier;

        var groups = reels
            .GroupBy(symbol => symbol.Id)
            .ToDictionary(group => group.Key, group => group.Count());

        if (groups.TryGetValue("cherry", out var cherries) && cherries == 2)
            return 1.50m;

        return groups.Any(group => group.Value == 2) ? 0.50m : 0m;
    }
}

public record SlotSymbol(
    string Id,
    string Name,
    string Icon,
    decimal TripleMultiplier,
    int Weight);

public record SlotResult(
    IReadOnlyList<SlotSymbol> Reels,
    decimal Multiplier,
    decimal Win);

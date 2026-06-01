using System.Security.Cryptography;

namespace CasinoRoyale.Services;

public class FruitsService
{
    private static readonly FruitSymbol[] Symbols =
    {
        new("cherry", "\U0001F352", "Wisnie", 3m),
        new("lemon", "\U0001F34B", "Cytryny", 4m),
        new("orange", "\U0001F34A", "Pomarancze", 5m),
        new("grape", "\U0001F347", "Winogrona", 7m),
        new("watermelon", "\U0001F349", "Arbuzy", 10m),
        new("seven", "7", "Siodemki", 20m)
    };

    public FruitsResult Spin(decimal bet)
    {
        var reels = Enumerable.Range(0, 5)
            .Select(_ => Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)])
            .ToArray();

        var bestGroup = reels
            .GroupBy(symbol => symbol.Id)
            .Select(group => new
            {
                Symbol = group.First(),
                Count = group.Count()
            })
            .OrderByDescending(group => group.Count)
            .ThenByDescending(group => group.Symbol.FiveOfAKindMultiplier)
            .First();

        var multiplier = CalculateMultiplier(bestGroup.Symbol, bestGroup.Count);
        var win = decimal.Round(bet * multiplier, 2);
        var message = multiplier switch
        {
            >= 20m => "Jackpot! Piec takich samych symboli.",
            >= 8m => "Mocne trafienie.",
            >= 2m => "Dobra seria.",
            > 0m => "Mala wygrana.",
            _ => "Tym razem bez wygranej."
        };

        return new FruitsResult(reels, multiplier, win, message);
    }

    public IReadOnlyList<FruitsPayout> GetPaytable()
    {
        return Symbols
            .Select(symbol => new FruitsPayout(
                symbol.Icon,
                symbol.Name,
                CalculateMultiplier(symbol, 3),
                CalculateMultiplier(symbol, 4),
                CalculateMultiplier(symbol, 5)))
            .ToArray();
    }

    private static decimal CalculateMultiplier(FruitSymbol symbol, int count)
    {
        return count switch
        {
            >= 5 => symbol.FiveOfAKindMultiplier,
            4 => decimal.Round(symbol.FiveOfAKindMultiplier * 0.35m, 2),
            3 => decimal.Round(symbol.FiveOfAKindMultiplier * 0.12m, 2),
            2 when symbol.Id == "seven" => 1.2m,
            _ => 0m
        };
    }
}

public record FruitSymbol(string Id, string Icon, string Name, decimal FiveOfAKindMultiplier);

public record FruitsPayout(string Icon, string Name, decimal Three, decimal Four, decimal Five);

public record FruitsResult(IReadOnlyList<FruitSymbol> Reels, decimal Multiplier, decimal Win, string Message);

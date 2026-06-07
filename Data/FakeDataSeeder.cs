using Bogus;
using CasinoRoyale.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CasinoRoyale.Data;

public static class FakeDataSeeder
{
    private const string EmailDomain = "faker.casinoroyale.test";
    private static readonly string[] GameNames = ["Blackjack", "Mines", "Fruits", "Dice", "Keno", "Roulette", "Baccarat", "Plinko"];

    public static async Task SeedAsync(Automaty db, int userCount = 25)
    {
        await SeedProvidersGamesAndCategoriesAsync(db);
        await SeedUsersAsync(db, userCount);
    }

    public static async Task SeedUsersAsync(Automaty db, int userCount = 25)
    {
        if (userCount <= 0)
        {
            return;
        }

        var existingFakeUsers = await db.Users
            .CountAsync(user => user.Email.EndsWith("@" + EmailDomain));

        //if (existingFakeUsers > 0) return;

        Randomizer.Seed = new Random();

        var users = CreateUsers(userCount, existingFakeUsers);

        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        db.LoginHistories.AddRange(CreateLoginHistory(users));
        db.BetRecords.AddRange(CreateBetRecords(users));
        db.BlackjackGames.AddRange(CreateBlackjackGames(users));
        db.BaccaratGames.AddRange(CreateBaccaratGames(users));
        db.MinesGames.AddRange(CreateMinesGames(users));
        db.PlinkoGames.AddRange(CreatePlinkoGames(users));
        db.RouletteGames.AddRange(CreateRouletteGames(users));
        db.CrashSessions.AddRange(CreateCrashSessions(users));
        db.DiceGames.AddRange(CreateDiceGames(users));
        db.KenoGames.AddRange(CreateKenoGames(users));

        await db.SaveChangesAsync();
    }

    public static async Task SeedProvidersGamesAndCategoriesAsync(Automaty db)
    {
        var providerNames = CatalogGames
            .Select(game => game.ProviderName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var categoryNames = CatalogGames
            .SelectMany(game => game.CategoryNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var providers = await db.AutomatProviderzy
            .Where(provider => providerNames.Contains(provider.Nazwa))
            .ToDictionaryAsync(provider => provider.Nazwa, StringComparer.OrdinalIgnoreCase);

        foreach (var providerName in providerNames)
        {
            if (providers.ContainsKey(providerName))
            {
                continue;
            }

            var provider = new AutomatProvider { Nazwa = providerName };
            db.AutomatProviderzy.Add(provider);
            providers[providerName] = provider;
        }

        var categories = await db.Kategorie
            .Where(category => categoryNames.Contains(category.Nazwa))
            .ToDictionaryAsync(category => category.Nazwa, StringComparer.OrdinalIgnoreCase);

        foreach (var categoryName in categoryNames)
        {
            if (categories.ContainsKey(categoryName))
            {
                continue;
            }

            var category = new Kategoria { Nazwa = categoryName };
            db.Kategorie.Add(category);
            categories[categoryName] = category;
        }

        await db.SaveChangesAsync();

        var gameNames = CatalogGames
            .Select(game => game.GameName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingGames = await db.AutomatyInfo
            .Include(game => game.Kategorie)
            .Include(game => game.Provider)
            .Where(game => gameNames.Contains(game.Nazwa))
            .ToDictionaryAsync(game => game.Nazwa, StringComparer.OrdinalIgnoreCase);

        foreach (var catalogGame in CatalogGames)
        {
            if (!providers.TryGetValue(catalogGame.ProviderName, out var provider))
            {
                continue;
            }

            var gameCategories = catalogGame.CategoryNames
                .Select(categoryName => categories[categoryName])
                .ToList();

            if (!existingGames.TryGetValue(catalogGame.GameName, out var existingGame))
            {
                var newGame = new AutomatInfo
                {
                    Nazwa = catalogGame.GameName,
                    Provider = provider,
                    Kategorie = gameCategories
                };

                db.AutomatyInfo.Add(newGame);
                existingGames[catalogGame.GameName] = newGame;
                continue;
            }

            foreach (var category in gameCategories)
            {
                var hasCategory = existingGame.Kategorie
                    .Any(existingCategory => existingCategory.Nazwa.Equals(category.Nazwa, StringComparison.OrdinalIgnoreCase));

                if (!hasCategory)
                {
                    existingGame.Kategorie.Add(category);
                }
            }
        }

        await db.SaveChangesAsync();
    }

    private static List<User> CreateUsers(int userCount, int startIndex = 0)
    {
        var index = startIndex + 1;

        var faker = new Faker<User>("pl")
            .RuleFor(user => user.Imie, fake => fake.Name.FirstName())
            .RuleFor(user => user.Nazwisko, fake => fake.Name.LastName())
            .RuleFor(user => user.Nazwa, fake => $"test_{fake.Internet.UserName().ToLowerInvariant()}_{index++}")
            .RuleFor(user => user.Email, (_, user) => $"{user.Nazwa}@{EmailDomain}")
            .RuleFor(user => user.HasloHash, _ => BCrypt.Net.BCrypt.HashPassword("Test123!"))
            .RuleFor(user => user.IsAdmin, _ => false)
            .RuleFor(user => user.KycStatus, fake => fake.PickRandom<UserKycStatus>())
            .RuleFor(user => user.Wallet, fake => new Wallet
            {
                BalanceReal = fake.Finance.Amount(25m, 5000m, 2),
                BalanceBonus = fake.Finance.Amount(0m, 750m, 2),
                WageringRequired = fake.Random.Bool(0.35f) ? fake.Finance.Amount(100m, 2500m, 2) : null,
                WageringProgress = fake.Random.Bool(0.35f) ? fake.Finance.Amount(0m, 1000m, 2) : null,
                BonusExpiresAt = fake.Random.Bool(0.35f) ? fake.Date.Future(1).ToUniversalTime() : null
            });

        return faker.Generate(userCount);
    }

    private static IEnumerable<LoginHistory> CreateLoginHistory(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            var loginCount = faker.Random.Int(1, 8);

            for (var i = 0; i < loginCount; i++)
            {
                yield return new LoginHistory
                {
                    UserId = user.Id,
                    IpAddress = faker.Internet.Ip(),
                    UserAgent = faker.Internet.UserAgent(),
                    Successful = faker.Random.Bool(0.9f),
                    EventType = faker.Random.Bool(0.85f) ? "login" : "failed_login",
                    LoggedAt = faker.Date.Recent(45).ToUniversalTime()
                };
            }
        }
    }

    private static IEnumerable<BetRecord> CreateBetRecords(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            var betCount = faker.Random.Int(4, 18);

            for (var i = 0; i < betCount; i++)
            {
                var amount = faker.Finance.Amount(5m, 250m, 2);
                var payout = faker.Random.Bool(0.42f) ? amount * faker.Random.Decimal(1.1m, 4.5m) : 0m;

                yield return new BetRecord
                {
                    UserId = user.Id,
                    Amount = amount,
                    AmountFromBonus = faker.Random.Bool(0.2f) ? faker.Finance.Amount(1m, amount, 2) : 0m,
                    SessionKey = faker.Random.Guid().ToString("N")[..16],
                    Settled = true,
                    CreatedAt = faker.Date.Recent(60).ToUniversalTime(),
                    GameName = faker.PickRandom(GameNames),
                    PayoutAmount = Math.Round(payout, 2)
                };
            }
        }
    }

    private static IEnumerable<DiceGame> CreateDiceGames(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            for (var i = 0; i < faker.Random.Int(5, 15); i++)
            {
                var bet = faker.Finance.Amount(5m, 200m, 2);
                var multiplier = faker.Random.Decimal(1.2m, 5m);

                yield return new DiceGame
                {
                    UserId = user.Id,
                    BetAmount = bet,
                    Mode = faker.PickRandom("over", "under"),
                    Target = faker.Random.Int(10, 90),
                    Roll = faker.Random.Int(1, 100),
                    Multiplier = Math.Round(multiplier, 2),
                    WinAmount = faker.Random.Bool(0.45f) ? Math.Round(bet * multiplier, 2) : 0m,
                    CreatedAt = faker.Date.Recent(45).ToUniversalTime()
                };
            }
        }
    }

    private static IEnumerable<BlackjackGame> CreateBlackjackGames(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            for (var i = 0; i < faker.Random.Int(5, 15); i++)
            {
                var deck = CreateDeck(faker);
                var playerHand = DealCards(deck, 2);
                var dealerHand = DealCards(deck, faker.Random.Int(2, 4));
                var result = faker.PickRandom("player_wins", "dealer_wins", "push", "blackjack");
                var bet = faker.Finance.Amount(10m, 300m, 2);

                yield return new BlackjackGame
                {
                    UserId = user.Id,
                    DeckJson = JsonSerializer.Serialize(deck),
                    PlayerHandJson = JsonSerializer.Serialize(playerHand),
                    DealerHandJson = JsonSerializer.Serialize(dealerHand),
                    SplitHandJson = "[]",
                    BetAmount = bet,
                    SplitBetAmount = 0m,
                    IsActive = false,
                    IsPlayerTurn = false,
                    IsSplitActive = false,
                    IsPlayingSplitHand = false,
                    PayoutProcessed = true,
                    Result = result,
                    WinAmount = result switch
                    {
                        "blackjack" => Math.Round(bet * 2.5m, 2),
                        "player_wins" => bet * 2m,
                        "push" => bet,
                        _ => 0m
                    },
                    CreatedAt = faker.Date.Recent(45).ToUniversalTime(),
                    FinishedAt = faker.Date.Recent(30).ToUniversalTime()
                };
            }
        }
    }

    private static IEnumerable<BaccaratGame> CreateBaccaratGames(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            for (var i = 0; i < faker.Random.Int(5, 15); i++)
            {
                var deck = CreateDeck(faker);
                var playerHand = DealCards(deck, faker.Random.Int(2, 3));
                var bankerHand = DealCards(deck, faker.Random.Int(2, 3));
                var playerValue = CalculateBaccaratValue(playerHand);
                var bankerValue = CalculateBaccaratValue(bankerHand);
                var result = playerValue > bankerValue
                    ? "player_wins"
                    : bankerValue > playerValue ? "banker_wins" : "tie";
                var betType = faker.PickRandom("player", "banker", "tie");
                var bet = faker.Finance.Amount(10m, 300m, 2);
                var winAmount = result switch
                {
                    "player_wins" when betType == "player" => bet * 2m,
                    "banker_wins" when betType == "banker" => Math.Round(bet * 1.95m, 2),
                    "tie" when betType == "tie" => bet * 9m,
                    "tie" => bet,
                    _ => 0m
                };

                yield return new BaccaratGame
                {
                    UserId = user.Id,
                    DeckJson = JsonSerializer.Serialize(deck),
                    PlayerHandJson = JsonSerializer.Serialize(playerHand),
                    BankerHandJson = JsonSerializer.Serialize(bankerHand),
                    BetAmount = bet,
                    BetType = betType,
                    PlayerValue = playerValue,
                    BankerValue = bankerValue,
                    Result = result,
                    WinAmount = winAmount,
                    IsActive = false,
                    CreatedAt = faker.Date.Recent(45).ToUniversalTime()
                };
            }
        }
    }

    private static IEnumerable<MinesGame> CreateMinesGames(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            for (var i = 0; i < faker.Random.Int(5, 15); i++)
            {
                var mineCount = faker.Random.Int(3, 12);
                var mines = faker.Random.Shuffle(Enumerable.Range(0, 25)).Take(mineCount).OrderBy(position => position).ToArray();
                var revealed = faker.Random.Shuffle(Enumerable.Range(0, 25).Except(mines)).Take(faker.Random.Int(1, 8)).OrderBy(position => position).ToArray();

                yield return new MinesGame
                {
                    UserId = user.Id,
                    GridSize = 25,
                    MineCount = mineCount,
                    MinePositions = string.Join(",", mines),
                    RevealedPositions = string.Join(",", revealed),
                    BetAmount = faker.Finance.Amount(5m, 200m, 2),
                    IsActive = false,
                    CreatedAt = faker.Date.Recent(45).ToUniversalTime()
                };
            }
        }
    }

    private static IEnumerable<PlinkoGame> CreatePlinkoGames(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            for (var i = 0; i < faker.Random.Int(5, 15); i++)
            {
                var bet = faker.Finance.Amount(5m, 200m, 2);
                var multiplier = faker.PickRandom(0m, 0.5m, 1.2m, 2m, 5m, 10m);

                yield return new PlinkoGame
                {
                    UserId = user.Id,
                    BetAmount = bet,
                    Risk = faker.PickRandom("low", "medium", "high"),
                    WinAmount = Math.Round(bet * multiplier, 2),
                    CreatedAt = faker.Date.Recent(45).ToUniversalTime()
                };
            }
        }
    }

    private static IEnumerable<RouletteGame> CreateRouletteGames(IEnumerable<User> users)
    {
        var faker = new Faker("pl");
        var betTypes = new[] { "red", "black", "odd", "even", "low", "high", "dozen1", "dozen2", "dozen3", "number" };

        foreach (var user in users)
        {
            for (var i = 0; i < faker.Random.Int(5, 15); i++)
            {
                var resultNumber = faker.Random.Int(0, 36);
                var betCount = faker.Random.Int(1, 3);
                var bets = Enumerable.Range(0, betCount)
                    .Select(_ =>
                    {
                        var type = faker.PickRandom(betTypes);
                        var value = type == "number" ? faker.Random.Int(0, 36).ToString() : string.Empty;
                        var bet = faker.Finance.Amount(5m, 150m, 2);
                        var win = CalculateRouletteWin(type, value, resultNumber, bet);

                        return new RouletteBetSeed(type, value, bet, win, win > 0);
                    })
                    .ToList();

                yield return new RouletteGame
                {
                    UserId = user.Id,
                    BetAmount = bets.Sum(bet => bet.Bet),
                    BetType = bets.Count == 1 ? bets[0].BetType : "multi",
                    BetValue = bets.Count == 1 ? bets[0].BetValue : string.Empty,
                    ResultNumber = resultNumber,
                    WinAmount = bets.Sum(bet => bet.Win),
                    BetsJson = JsonSerializer.Serialize(bets),
                    CreatedAt = faker.Date.Recent(45).ToUniversalTime()
                };
            }
        }
    }

    private static IEnumerable<CrashSession> CreateCrashSessions(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            for (var i = 0; i < faker.Random.Int(5, 15); i++)
            {
                var bet = faker.Finance.Amount(5m, 250m, 2);
                var crashPoint = Math.Round(faker.Random.Decimal(1.01m, 12m), 2);
                var cashedOut = faker.Random.Bool(0.45f);
                var cashoutMultiplier = cashedOut
                    ? Math.Round(faker.Random.Decimal(1.01m, Math.Max(1.01m, crashPoint - 0.01m)), 2)
                    : (decimal?)null;

                yield return new CrashSession
                {
                    UserId = user.Id,
                    BetAmount = bet,
                    CrashPoint = crashPoint,
                    StartTime = faker.Date.Recent(45).ToUniversalTime(),
                    IsActive = false,
                    CashoutMultiplier = cashoutMultiplier,
                    WinAmount = cashoutMultiplier.HasValue ? Math.Round(bet * cashoutMultiplier.Value, 2) : 0m,
                    CreatedAt = faker.Date.Recent(45).ToUniversalTime()
                };
            }
        }
    }

    private static IEnumerable<KenoGame> CreateKenoGames(IEnumerable<User> users)
    {
        var faker = new Faker("pl");

        foreach (var user in users)
        {
            for (var i = 0; i < faker.Random.Int(5, 15); i++)
            {
                var selected = faker.Random.Shuffle(Enumerable.Range(1, 40)).Take(8).OrderBy(number => number).ToArray();
                var drawn = faker.Random.Shuffle(Enumerable.Range(1, 40)).Take(10).OrderBy(number => number).ToArray();
                var hits = selected.Intersect(drawn).Count();
                var bet = faker.Finance.Amount(5m, 150m, 2);
                var multiplier = hits >= 3 ? hits * 0.75m : 0m;

                yield return new KenoGame
                {
                    UserId = user.Id,
                    BetAmount = bet,
                    SelectedNumbers = string.Join(",", selected),
                    DrawnNumbers = string.Join(",", drawn),
                    Hits = hits,
                    Multiplier = multiplier,
                    WinAmount = Math.Round(bet * multiplier, 2),
                    CreatedAt = faker.Date.Recent(45).ToUniversalTime()
                };
            }
        }
    }

    private static List<BlackjackCard> CreateDeck(Faker faker)
    {
        var suits = new[] { "hearts", "diamonds", "clubs", "spades" };
        var ranks = new[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        return faker.Random.Shuffle(
                suits.SelectMany(suit => ranks.Select(rank => new BlackjackCard { Suit = suit, Rank = rank })))
            .ToList();
    }

    private static List<BlackjackCard> DealCards(List<BlackjackCard> deck, int count)
    {
        var cards = deck.Take(count).ToList();
        deck.RemoveRange(0, count);
        return cards;
    }

    private static int CalculateBaccaratValue(IEnumerable<BlackjackCard> hand)
    {
        return hand.Sum(card => card.Rank switch
        {
            "A" => 1,
            "10" or "J" or "Q" or "K" => 0,
            _ => int.Parse(card.Rank)
        }) % 10;
    }

    private static decimal CalculateRouletteWin(string betType, string betValue, int number, decimal bet)
    {
        var redNumbers = new HashSet<int> { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

        var wins = betType switch
        {
            "number" => int.TryParse(betValue, out var selected) && selected == number,
            "red" => redNumbers.Contains(number),
            "black" => number != 0 && !redNumbers.Contains(number),
            "odd" => number != 0 && number % 2 == 1,
            "even" => number != 0 && number % 2 == 0,
            "low" => number is >= 1 and <= 18,
            "high" => number is >= 19 and <= 36,
            "dozen1" => number is >= 1 and <= 12,
            "dozen2" => number is >= 13 and <= 24,
            "dozen3" => number is >= 25 and <= 36,
            _ => false
        };

        if (!wins)
        {
            return 0m;
        }

        return betType == "number" || betType.StartsWith("dozen", StringComparison.Ordinal)
            ? bet * (betType == "number" ? 36m : 3m)
            : bet * 2m;
    }

    private static readonly CatalogGameSeed[] CatalogGames =
    [
        new("Book of Pharaoh", "NetEnt", ["Sloty", "Egipt", "Bonus Buy"]),
        new("Starburst Royale", "NetEnt", ["Sloty", "Klasyki", "Jackpot"]),
        new("Gonzo's Quest Gold", "NetEnt", ["Sloty", "Przygodowe", "Megaways"]),
        new("Sweet Bonanza", "Pragmatic Play", ["Sloty", "Cukierkowe", "Bonus Buy"]),
        new("Wolf Gold", "Pragmatic Play", ["Sloty", "Jackpot", "Zwierzeta"]),
        new("The Dog House", "Pragmatic Play", ["Sloty", "Bonus Buy", "Zwierzeta"]),
        new("Legacy of Dead", "Play'n GO", ["Sloty", "Egipt", "Wysoka wariancja"]),
        new("Reactoonz", "Play'n GO", ["Sloty", "Cluster Pays", "Kosmos"]),
        new("Moon Princess", "Play'n GO", ["Sloty", "Fantasy", "Cluster Pays"]),
        new("Wanted Dead or a Wild", "Hacksaw Gaming", ["Sloty", "Western", "Wysoka wariancja"]),
        new("Chaos Crew", "Hacksaw Gaming", ["Sloty", "Bonus Buy", "Wysoka wariancja"]),
        new("Hand of Anubis", "Hacksaw Gaming", ["Sloty", "Egipt", "Bonus Buy"]),
        new("Big Bass Splash", "Pragmatic Play", ["Sloty", "Wedkarskie", "Bonus Buy"]),
        new("Fire Joker", "Play'n GO", ["Sloty", "Klasyki", "Owoce"]),
        new("Mega Moolah", "Microgaming", ["Sloty", "Jackpot", "Klasyki"]),
        new("Immortal Romance", "Microgaming", ["Sloty", "Fantasy", "Klasyki"]),
        new("Fruit Party", "Pragmatic Play", ["Sloty", "Owoce", "Cluster Pays"]),
        new("Jammin' Jars", "Push Gaming", ["Sloty", "Owoce", "Cluster Pays"]),
        new("Razor Shark", "Push Gaming", ["Sloty", "Morskie", "Wysoka wariancja"]),
        new("Mental", "Nolimit City", ["Sloty", "Bonus Buy", "Wysoka wariancja"])
    ];

    private sealed record CatalogGameSeed(string GameName, string ProviderName, string[] CategoryNames);
    private sealed record RouletteBetSeed(string BetType, string BetValue, decimal Bet, decimal Win, bool Won);
}

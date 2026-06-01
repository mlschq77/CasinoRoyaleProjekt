using CasinoRoyale.Data;
using CasinoRoyale.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CasinoRoyale.Services
{
    public class BaccaratService
    {
        private static readonly string[] Suits = { "hearts", "diamonds", "clubs", "spades" };
        private static readonly string[] Ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        private readonly Automaty _db;
        private readonly IBalanceService _balanceService;

        public BaccaratService(Automaty db, IBalanceService balanceService)
        {
            _db = db;
            _balanceService = balanceService;
        }

        // ── Podstawowe operacje na kartach (identyczne z BlackjackService) ──

        private List<BlackjackCard> CreateShuffledDeck()
        {
            var deck = new List<BlackjackCard>();
            foreach (var suit in Suits)
                foreach (var rank in Ranks)
                    deck.Add(new BlackjackCard { Suit = suit, Rank = rank });

            var rng = new Random();
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
            return deck;
        }

        private BlackjackCard DealCard(List<BlackjackCard> deck)
        {
            var card = deck[0];
            deck.RemoveAt(0);
            return card;
        }

        // ── Obliczanie wartości w Baccarat ──
        // A = 1, 10/J/Q/K = 0, 2-9 = wartość nominalna, wynik = suma % 10

        public int CalculateHandValue(List<BlackjackCard> hand)
        {
            int total = 0;

            foreach (var card in hand)
                total += GetBaccaratCardValue(card);

            return total % 10;
        }

        private bool IsNatural(List<BlackjackCard> hand)
        {
            return hand.Count == 2 && CalculateHandValue(hand) >= 8;
        }

        // ── REGUŁA TRZECIEJ KARTY (TABLEAU) ──

        private bool ShouldPlayerDraw(List<BlackjackCard> playerHand)
        {
            int value = CalculateHandValue(playerHand);
            return value <= 5;
        }

        private bool ShouldBankerDraw(List<BlackjackCard> bankerHand, List<BlackjackCard> playerHand, bool playerDrew)
        {
            int bankerValue = CalculateHandValue(bankerHand);

            if (bankerValue >= 7)
                return false;

            if (!playerDrew)
            {
                return bankerValue <= 5;
            }

            int playerThirdCardValue = GetBaccaratCardValue(playerHand[2]);

            return bankerValue switch
            {
                <= 2 => true,
                3 => playerThirdCardValue != 8,
                4 => playerThirdCardValue >= 2 && playerThirdCardValue <= 7,
                5 => playerThirdCardValue >= 4 && playerThirdCardValue <= 7,
                6 => playerThirdCardValue == 6 || playerThirdCardValue == 7,
                _ => false
            };
        }

        private int GetBaccaratCardValue(BlackjackCard card)
        {
            if (card.Rank == "A") return 1;
            if (card.Rank == "J" || card.Rank == "Q" || card.Rank == "K" || card.Rank == "10") return 0;
            return int.Parse(card.Rank);
        }

        // ── Główna logika rozgrywki ──

        public async Task<(bool success, string error, BaccaratGame? game, decimal balance)> PlayRoundAsync(
            int userId, decimal bet, string betType)
        {
            if (bet <= 0)
                return (false, "Stawka musi byc wieksza od zera.", null, 0);

            if (betType != "player" && betType != "banker" && betType != "tie")
                return (false, "Nieprawidlowy typ zakladu (player/banker/tie).", null, 0);

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                // 1. Pobranie zakładu
                var sessionKey = "bac:" + Guid.NewGuid().ToString("N");
                var betResult = await _balanceService.PlaceBetAsync(userId, bet, sessionKey);
                if (!betResult.Success)
                {
                    await tx.RollbackAsync();
                    return (false, betResult.Error ?? "Blad platnosci.", (BaccaratGame?)null, betResult.Balance);
                }

                // 2. Tasowanie i rozdanie (kolejność: Player → Banker → Player → Banker)
                var deck = CreateShuffledDeck();
                var playerHand = new List<BlackjackCard> { DealCard(deck) };
                var bankerHand = new List<BlackjackCard> { DealCard(deck) };
                playerHand.Add(DealCard(deck));
                bankerHand.Add(DealCard(deck));

                int playerValue = CalculateHandValue(playerHand);
                int bankerValue = CalculateHandValue(bankerHand);

                // 3. Sprawdzenie naturali (8 lub 9)
                bool playerNatural = IsNatural(playerHand);
                bool bankerNatural = IsNatural(bankerHand);

                if (!playerNatural && !bankerNatural)
                {
                    // 4. Reguła trzeciej karty dla Playera
                    bool playerDrew = false;
                    if (ShouldPlayerDraw(playerHand))
                    {
                        playerHand.Add(DealCard(deck));
                        playerDrew = true;
                    }

                    // 5. Reguła trzeciej karty dla Bankera
                    if (ShouldBankerDraw(bankerHand, playerHand, playerDrew))
                    {
                        bankerHand.Add(DealCard(deck));
                    }
                }

                // 6. Obliczenie ostatecznych wartości
                playerValue = CalculateHandValue(playerHand);
                bankerValue = CalculateHandValue(bankerHand);

                // 7. Rozstrzygnięcie
                string result;
                decimal winAmount;

                if (playerValue > bankerValue)
                    result = "player_wins";
                else if (bankerValue > playerValue)
                    result = "banker_wins";
                else
                    result = "tie";

                // 8. Obliczenie wygranej
                // result = "player_wins" / "banker_wins" / "tie"
                // betType = "player" / "banker" / "tie"
                if ((result == "player_wins" && betType == "player") ||
                    (result == "banker_wins" && betType == "banker") ||
                    (result == "tie" && betType == "tie"))
                {
                    winAmount = betType switch
                    {
                        "player" => bet * 2m,
                        "banker" => bet * 1.95m,
                        "tie" => bet * 9m,
                        _ => 0
                    };
                }
                else if (result == "tie")
                {
                    // Push — zwrot stawki
                    winAmount = bet;
                }
                else
                {
                    winAmount = 0;
                }

                // 9. Zapis gry do bazy
                var game = new BaccaratGame
                {
                    UserId = userId,
                    BetAmount = bet,
                    BetType = betType,
                    DeckJson = JsonConvert.SerializeObject(deck),
                    PlayerHandJson = JsonConvert.SerializeObject(playerHand),
                    BankerHandJson = JsonConvert.SerializeObject(bankerHand),
                    PlayerValue = playerValue,
                    BankerValue = bankerValue,
                    Result = result,
                    WinAmount = winAmount,
                    IsActive = false
                };

                _db.BaccaratGames.Add(game);
                await _db.SaveChangesAsync();

                // 10. Wypłata wygranej
                if (winAmount > 0)
                {
                    var payoutResult = await _balanceService.PayoutAsync(userId, winAmount, sessionKey);
                    if (!payoutResult.Success)
                    {
                        await tx.RollbackAsync();
                        return (false, payoutResult.Error ?? "Blad wyplaty.", null, 0);
                    }
                }

                await tx.CommitAsync();

                var finalBalance = await _balanceService.GetBalanceAsync(userId) ?? 0;
                return (true, "", game, finalBalance);
            });
        }

        // ── Budowanie stanu gry do frontendu ──

        public object BuildGameState(BaccaratGame game)
        {
            var playerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.PlayerHandJson)!;
            var bankerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.BankerHandJson)!;

            decimal netWin = game.WinAmount - game.BetAmount;

            return new
            {
                gameId = game.Id,
                playerHand,
                bankerHand,
                playerValue = game.PlayerValue,
                bankerValue = game.BankerValue,
                bet = game.BetAmount,
                betType = game.BetType,
                result = game.Result,
                winAmount = game.WinAmount,
                netWin,
                isActive = game.IsActive,
                createdAt = game.CreatedAt
            };
        }
    }
}

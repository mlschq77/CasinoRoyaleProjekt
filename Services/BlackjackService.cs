using CasinoRoyale.Data;
using CasinoRoyale.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CasinoRoyale.Services
{
    public class BlackjackService
    {
        private static readonly string[] Suits = { "hearts", "diamonds", "clubs", "spades" };
        private static readonly string[] Ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        private readonly Automaty _db;
        private readonly IBalanceService _balanceService;

        public BlackjackService(Automaty db, IBalanceService balanceService)
        {
            _db = db;
            _balanceService = balanceService;
        }

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

        public int CalculateHandValue(List<BlackjackCard> hand)
        {
            int total = 0;
            int aces = 0;

            foreach (var card in hand)
            {
                if (card.FaceDown) continue;
                if (card.Rank == "A")
                {
                    aces++;
                    total += 11;
                }
                else if (card.Rank == "J" || card.Rank == "Q" || card.Rank == "K")
                {
                    total += 10;
                }
                else
                {
                    total += int.Parse(card.Rank);
                }
            }

            while (total > 21 && aces > 0)
            {
                total -= 10;
                aces--;
            }

            return total;
        }

        private bool IsBlackjack(List<BlackjackCard> hand)
        {
            return hand.Count == 2 && CalculateHandValue(hand) == 21;
        }

        private bool IsBust(List<BlackjackCard> hand)
        {
            return CalculateHandValue(hand) > 21;
        }

        public async Task<(bool success, string error, BlackjackGame? game, decimal balance)> StartGameAsync(int userId, decimal bet)
        {
            if (bet <= 0)
                return (false, "Stawka musi byc wieksza od zera.", null, 0);

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                var existingActive = await _db.BlackjackGames
                    .Where(g => g.UserId == userId && g.IsActive)
                    .FirstOrDefaultAsync();

                if (existingActive != null)
                {
                    existingActive.IsActive = false;
                    existingActive.Result = "abandoned";
                    existingActive.FinishedAt = DateTime.UtcNow;
                }                    var deck = CreateShuffledDeck();

                var playerCard1 = DealCard(deck);
                var dealerCard1 = DealCard(deck);
                var playerCard2 = DealCard(deck);
                var dealerCard2Hidden = DealCard(deck);
                dealerCard2Hidden.FaceDown = true;

                var playerHand = new List<BlackjackCard> { playerCard1, playerCard2 };
                var dealerHand = new List<BlackjackCard> { dealerCard1, dealerCard2Hidden };

                var game = new BlackjackGame
                {
                    UserId = userId,
                    BetAmount = bet,
                    DeckJson = JsonConvert.SerializeObject(deck),
                    PlayerHandJson = JsonConvert.SerializeObject(playerHand),
                    DealerHandJson = JsonConvert.SerializeObject(dealerHand),
                    SplitHandJson = "[]",
                    IsActive = true,
                    IsPlayerTurn = true
                };

                _db.BlackjackGames.Add(game);
                await _db.SaveChangesAsync();

                // Zapisz grę PRZED PlaceBet — potrzebujemy game.Id do sessionKey
                var sessionKey = "bj:" + game.Id;
                var betResult = await _balanceService.PlaceBetAsync(userId, bet, sessionKey);
                if (!betResult.Success)
                {
                    await tx.RollbackAsync();
                    return (false, betResult.Error ?? "Blad platnosci.", (BlackjackGame?)null, betResult.Balance);
                }

                await tx.CommitAsync();

                return (true, "", game, betResult.Balance);
            });
        }

        public async Task<(bool success, string error, BlackjackGame? game, decimal balance)> HitAsync(int userId, int gameId)
        {
            var game = await _db.BlackjackGames.FindAsync(gameId);

            if (game == null || game.UserId != userId || !game.IsActive || !game.IsPlayerTurn)
                return (false, "Nieprawidlowa akcja.", null, 0);

            var deck = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.DeckJson)!;
            decimal balance = 0;

            if (game.IsPlayingSplitHand)
            {
                var splitHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.SplitHandJson)!;
                splitHand.Add(DealCard(deck));
                game.SplitHandJson = JsonConvert.SerializeObject(splitHand);
                game.DeckJson = JsonConvert.SerializeObject(deck);

                if (IsBust(splitHand))
                {
                    game.IsPlayerTurn = false;
                    (game, balance) = await RunDealerAndFinishAsync(game);
                }
            }
            else
            {
                var playerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.PlayerHandJson)!;
                playerHand.Add(DealCard(deck));
                game.PlayerHandJson = JsonConvert.SerializeObject(playerHand);
                game.DeckJson = JsonConvert.SerializeObject(deck);

                if (IsBust(playerHand))
                {
                    if (game.IsSplitActive)
                        game.IsPlayingSplitHand = true;
                    else
                    {
                        game.IsPlayerTurn = false;
                        (game, balance) = await RunDealerAndFinishAsync(game);
                    }
                }
            }

            await _db.SaveChangesAsync();
            return (true, "", game, balance);
        }

        public async Task<(bool success, string error, BlackjackGame? game, decimal balance)> StandAsync(int userId, int gameId)
        {
            var game = await _db.BlackjackGames.FindAsync(gameId);

            if (game == null || game.UserId != userId || !game.IsActive || !game.IsPlayerTurn)
                return (false, "Nieprawidlowa akcja.", null, 0);

            if (!game.IsPlayingSplitHand && game.IsSplitActive)
            {
                game.IsPlayingSplitHand = true;
                await _db.SaveChangesAsync();
                return (true, "", game, 0);
            }

            game.IsPlayerTurn = false;
            var (finalGame, balance) = await RunDealerAndFinishAsync(game);

            return (true, "", finalGame, balance);
        }

        public async Task<(bool success, string error, BlackjackGame? game, decimal balance)> DoubleDownAsync(int userId, int gameId, bool faceDown)
        {
            var game = await _db.BlackjackGames.FindAsync(gameId);

            if (game == null || game.UserId != userId || !game.IsActive || !game.IsPlayerTurn)
                return (false, "Nieprawidlowa akcja.", null, 0);

            var playerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.PlayerHandJson)!;

            if (playerHand.Count != 2)
                return (false, "Double down mozliwy tylko po pierwszych dwoch kartach.", null, 0);

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                var sessionKey = "bj:" + game.Id;
                var betResult = await _balanceService.PlaceBetAsync(userId, game.BetAmount, sessionKey);
                if (!betResult.Success)
                    return (false, betResult.Error ?? "Brak srodkow na double.", (BlackjackGame?)null, betResult.Balance);

                game.BetAmount *= 2;

                var deck = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.DeckJson)!;
                var hand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.PlayerHandJson)!;
                hand.Add(DealCard(deck));
                game.PlayerHandJson = JsonConvert.SerializeObject(hand);
                game.DeckJson = JsonConvert.SerializeObject(deck);
                game.IsPlayerTurn = false;

                var (finalGame, winBalance) = await RunDealerAndFinishAsync(game);
                await tx.CommitAsync();

                return (true, "", finalGame, winBalance);
            });
        }

        public async Task<(bool success, string error, BlackjackGame? game, decimal balance)> SplitAsync(int userId, int gameId)
        {
            var game = await _db.BlackjackGames.FindAsync(gameId);

            if (game == null || game.UserId != userId || !game.IsActive || !game.IsPlayerTurn)
                return (false, "Nieprawidlowa akcja.", null, 0);

            if (game.IsSplitActive)
                return (false, "Split juz aktywny.", null, 0);

            var playerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.PlayerHandJson)!;

            if (playerHand.Count != 2 || playerHand[0].GetValue() != playerHand[1].GetValue())
                return (false, "Split mozliwy tylko gdy obie karty maja ta sama wartosc.", null, 0);

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync();

                var sessionKey = "bj:" + game.Id;
                var betResult = await _balanceService.PlaceBetAsync(userId, game.BetAmount, sessionKey);
                if (!betResult.Success)
                    return (false, betResult.Error ?? "Brak srodkow na split.", (BlackjackGame?)null, betResult.Balance);

                game.SplitBetAmount = game.BetAmount;
                var deck = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.DeckJson)!;
                var hand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.PlayerHandJson)!;

                var firstHand = new List<BlackjackCard> { hand[0], DealCard(deck) };
                var secondHand = new List<BlackjackCard> { hand[1], DealCard(deck) };

                game.PlayerHandJson = JsonConvert.SerializeObject(firstHand);
                game.SplitHandJson = JsonConvert.SerializeObject(secondHand);
                game.DeckJson = JsonConvert.SerializeObject(deck);
                game.IsSplitActive = true;
                game.IsPlayingSplitHand = false;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, "", game, betResult.Balance);
            });
        }

        private async Task<(BlackjackGame game, decimal balance)> RunDealerAndFinishAsync(BlackjackGame game)
        {
            var deck = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.DeckJson)!;
            var dealerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.DealerHandJson)!;
            var playerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.PlayerHandJson)!;

            foreach (var card in dealerHand)
                card.FaceDown = false;

            while (CalculateHandValue(dealerHand) < 17)
                dealerHand.Add(DealCard(deck));

            game.DealerHandJson = JsonConvert.SerializeObject(dealerHand);
            game.DeckJson = JsonConvert.SerializeObject(deck);

            int playerVal = CalculateHandValue(playerHand);
            int dealerVal = CalculateHandValue(dealerHand);
            bool playerBlackjack = IsBlackjack(playerHand);
            bool dealerBlackjack = IsBlackjack(dealerHand);

            decimal totalWin;
            string result;

            if (!game.IsSplitActive)
            {
                if (playerBlackjack && dealerBlackjack)
                {
                    result = "push";
                    totalWin = game.BetAmount;
                }
                else if (playerBlackjack)
                {
                    result = "blackjack";
                    totalWin = game.BetAmount + game.BetAmount * 1.5m;
                }
                else if (dealerBlackjack || IsBust(playerHand))
                {
                    result = "dealer_wins";
                    totalWin = 0;
                }
                else if (IsBust(dealerHand))
                {
                    result = "player_wins";
                    totalWin = game.BetAmount * 2;
                }
                else if (playerVal > dealerVal)
                {
                    result = "player_wins";
                    totalWin = game.BetAmount * 2;
                }
                else if (playerVal < dealerVal)
                {
                    result = "dealer_wins";
                    totalWin = 0;
                }
                else
                {
                    result = "push";
                    totalWin = game.BetAmount;
                }
            }
            else
            {
                var splitHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.SplitHandJson)!;
                int splitVal = CalculateHandValue(splitHand);

                decimal mainWin = ResolveHand(playerVal, dealerVal, IsBust(playerHand), IsBust(dealerHand), game.BetAmount);
                decimal splitWin = ResolveHand(splitVal, dealerVal, IsBust(splitHand), IsBust(dealerHand), game.SplitBetAmount);
                totalWin = mainWin + splitWin;

                decimal totalBet = game.BetAmount + game.SplitBetAmount;
                if (totalWin > totalBet) result = "player_wins";
                else if (totalWin == totalBet) result = "push";
                else result = "dealer_wins";
            }

            game.Result = result;
            game.WinAmount = totalWin;
            game.IsActive = false;
            game.IsPlayerTurn = false;
            game.PayoutProcessed = true;
            game.FinishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            decimal finalBalance;
            if (totalWin > 0)
            {
                var sessionKey = "bj:" + game.Id;
                var payoutResult = await _balanceService.PayoutAsync(game.UserId, totalWin, sessionKey);
                finalBalance = payoutResult.Balance;
            }
            else
            {
                finalBalance = (await _balanceService.GetBalanceAsync(game.UserId)) ?? 0;
            }

            return (game, finalBalance);
        }

        private decimal ResolveHand(int playerVal, int dealerVal, bool playerBust, bool dealerBust, decimal bet)
        {
            if (playerBust) return 0;
            if (dealerBust) return bet * 2;
            if (playerVal > dealerVal) return bet * 2;
            if (playerVal == dealerVal) return bet;
            return 0;
        }

        private object ResolveHandState(int playerVal, int dealerVal, bool playerBust, bool dealerBust, decimal bet)
        {
            var winAmount = ResolveHand(playerVal, dealerVal, playerBust, dealerBust, bet);
            string result;

            if (winAmount > bet)
                result = "win";
            else if (winAmount == bet)
                result = "push";
            else
                result = "lose";

            return new
            {
                result,
                winAmount,
                netAmount = winAmount - bet
            };
        }

        public object BuildGameState(BlackjackGame game)
        {
            var playerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.PlayerHandJson)!;
            var dealerHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.DealerHandJson)!;
            var splitHand = JsonConvert.DeserializeObject<List<BlackjackCard>>(game.SplitHandJson ?? "[]") ?? new List<BlackjackCard>();

            var visibleDealerHand = dealerHand.Select(c => new BlackjackCard
            {
                Suit = c.FaceDown ? "" : c.Suit,
                Rank = c.FaceDown ? "?" : c.Rank,
                FaceDown = c.FaceDown
            }).ToList();

            int playerValue = CalculateHandValue(playerHand);
            int dealerVisibleValue = CalculateHandValue(visibleDealerHand.Where(c => !c.FaceDown).ToList());
            int splitValue = splitHand.Count > 0 ? CalculateHandValue(splitHand) : 0;

            bool canDouble = playerHand.Count == 2 && game.IsPlayerTurn && !game.IsPlayingSplitHand;
            bool canSplit = playerHand.Count == 2 && !game.IsSplitActive && game.IsPlayerTurn
                            && !game.IsPlayingSplitHand && playerHand[0].GetValue() == playerHand[1].GetValue();

            string activeHand = game.IsSplitActive && game.IsPlayingSplitHand ? "split" : "main";
            object? handResults = null;

            if (!game.IsActive && game.IsSplitActive)
            {
                int dealerFullValue = CalculateHandValue(dealerHand);

                handResults = new
                {
                    main = ResolveHandState(playerValue, dealerFullValue, IsBust(playerHand), IsBust(dealerHand), game.BetAmount),
                    split = ResolveHandState(splitValue, dealerFullValue, IsBust(splitHand), IsBust(dealerHand), game.SplitBetAmount)
                };
            }

            return new
            {
                gameId = game.Id,
                isActive = game.IsActive,
                isPlayerTurn = game.IsPlayerTurn,
                isSplitActive = game.IsSplitActive,
                isPlayingSplitHand = game.IsPlayingSplitHand,
                activeHand,
                playerHand,
                dealerHand = visibleDealerHand,
                splitHand,
                playerValue,
                dealerValue = dealerVisibleValue,
                splitValue,
                bet = game.BetAmount,
                splitBet = game.SplitBetAmount,
                canDouble,
                canSplit,
                result = game.Result,
                winAmount = game.WinAmount,
                handResults
            };
        }
    }
}

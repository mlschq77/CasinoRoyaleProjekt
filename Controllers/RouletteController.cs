using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace CasinoRoyale.Controllers;

[Authorize]
[ApiController]
[Route("api/roulette")]
public class RouletteController : ControllerBase
{
    private readonly Automaty _db;
    private readonly IBalanceService _balanceService;
    private readonly RouletteService _rouletteService;

    private static readonly HashSet<string> SimpleTypes = new()
    {
        "red", "black", "odd", "even", "low", "high",
        "dozen1", "dozen2", "dozen3", "column1", "column2", "column3"
    };

    private static readonly HashSet<string> MultiNumberTypes = new()
    {
        "split", "street", "corner", "sixline"
    };

    public RouletteController(Automaty db, IBalanceService balanceService, RouletteService rouletteService)
    {
        _db = db;
        _balanceService = balanceService;
        _rouletteService = rouletteService;
    }

    [HttpPost("spin")]
    public async Task<IActionResult> Spin([FromBody] RouletteSpinRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (request.Bets == null || request.Bets.Count == 0)
            return BadRequest(new { error = "Brak zakładów." });

        foreach (var entry in request.Bets)
        {
            if (entry.Bet <= 0)
                return BadRequest(new { error = "Każda stawka musi być większa od zera." });
            if (entry.Bet > 1_000_000)
                return BadRequest(new { error = "Maksymalna stawka to 1 000 000." });

            if (entry.BetType == "number")
            {
                if (!int.TryParse(entry.BetValue, out var num) || num < 0 || num > 36)
                    return BadRequest(new { error = "Nieprawidłowy numer (0–36)." });
            }
            else if (MultiNumberTypes.Contains(entry.BetType))
            {
                if (string.IsNullOrEmpty(entry.BetValue))
                    return BadRequest(new { error = $"Brak wartości dla zakładu {entry.BetType}." });
                var parts = entry.BetValue.Split('-');
                if (!parts.All(p => int.TryParse(p, out var n) && n >= 0 && n <= 36))
                    return BadRequest(new { error = $"Nieprawidłowe numery dla zakładu {entry.BetType}." });
            }
            else if (!SimpleTypes.Contains(entry.BetType))
            {
                return BadRequest(new { error = $"Nieprawidłowy typ zakładu: {entry.BetType}" });
            }
        }

        var totalBet = request.Bets.Sum(b => b.Bet);

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var betResult = await _balanceService.PlaceBetAsync(userId.Value, totalBet);
            if (!betResult.Success)
                return BadRequest(new { error = betResult.Error, balance = betResult.Balance });

            var number = _rouletteService.Spin();
            var color  = _rouletteService.GetColor(number);

            var results = request.Bets.Select(b =>
            {
                var win = _rouletteService.CalculateWin(b.BetType, b.BetValue ?? "", number, b.Bet);
                return new BetResult(b.BetType, b.BetValue ?? "", b.Bet, win, win > 0);
            }).ToList();

            var totalWin = results.Sum(r => r.Win);

            _db.RouletteGames.Add(new RouletteGame
            {
                UserId       = userId.Value,
                BetAmount    = totalBet,
                BetType      = request.Bets.Count == 1 ? request.Bets[0].BetType : "multi",
                BetValue     = request.Bets.Count == 1 ? (request.Bets[0].BetValue ?? "") : "",
                ResultNumber = number,
                WinAmount    = totalWin,
                BetsJson     = JsonSerializer.Serialize(results)
            });
            await _db.SaveChangesAsync();

            decimal finalBalance;
            if (totalWin > 0)
            {
                var payoutResult = await _balanceService.PayoutAsync(userId.Value, totalWin);
                if (!payoutResult.Success)
                    return BadRequest(new { error = payoutResult.Error });
                finalBalance = payoutResult.Balance;
            }
            else
            {
                finalBalance = (await _balanceService.GetBalanceAsync(userId.Value)) ?? 0;
            }

            await transaction.CommitAsync();

            return Ok(new { number, color, totalWin, balance = finalBalance, results });
        }, null, CancellationToken.None);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}

public record BetEntry(string BetType, string? BetValue, decimal Bet);
public record BetResult(string BetType, string BetValue, decimal Bet, decimal Win, bool Won);
public record RouletteSpinRequest(List<BetEntry> Bets);

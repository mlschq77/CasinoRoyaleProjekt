using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
[ApiController]
[Route("api/roulette")]
public class RouletteController : ControllerBase
{
    private readonly Automaty _db;
    private readonly IBalanceService _balanceService;
    private readonly RouletteService _rouletteService;

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

        if (request.Bet <= 0)
            return BadRequest(new { error = "Stawka musi byc wieksza od zera." });

        if (request.Bet > 100000)
            return BadRequest(new { error = "Maksymalna stawka to 100000." });

        var validTypes = new[] { "number", "red", "black", "odd", "even", "low", "high", "dozen1", "dozen2", "dozen3", "column1", "column2", "column3" };
        if (!validTypes.Contains(request.BetType))
            return BadRequest(new { error = "Nieprawidlowy typ zakladu." });

        if (request.BetType == "number")
        {
            if (!int.TryParse(request.BetValue, out var num) || num < 0 || num > 36)
                return BadRequest(new { error = "Nieprawidlowy numer." });
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var betResult = await _balanceService.PlaceBetAsync(userId.Value, request.Bet);
            if (!betResult.Success)
                return BadRequest(new { error = betResult.Error, balance = betResult.Balance });

            var number = _rouletteService.Spin();
            var win = _rouletteService.CalculateWin(request.BetType, request.BetValue ?? "", number, request.Bet);

            _db.RouletteGames.Add(new RouletteGame
            {
                UserId = userId.Value,
                BetAmount = request.Bet,
                BetType = request.BetType,
                BetValue = request.BetValue ?? "",
                ResultNumber = number,
                WinAmount = win
            });
            await _db.SaveChangesAsync();

            decimal finalBalance;
            if (win > 0)
            {
                var payoutResult = await _balanceService.PayoutAsync(userId.Value, win);
                if (!payoutResult.Success)
                    return BadRequest(new { error = payoutResult.Error });
                finalBalance = payoutResult.Balance;
            }
            else
            {
                finalBalance = (await _balanceService.GetBalanceAsync(userId.Value)) ?? 0;
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                number,
                color = _rouletteService.GetColor(number),
                win,
                balance = finalBalance
            });
        }, null, CancellationToken.None);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }
}

public record RouletteSpinRequest(decimal Bet, string BetType, string? BetValue);

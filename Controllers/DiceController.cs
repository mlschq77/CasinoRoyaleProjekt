using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
[ApiController]
[Route("api/dice")]
public class DiceController : ControllerBase
{
    private readonly Automaty _db;
    private readonly IBalanceService _balanceService;
    private readonly DiceService _diceService;

    public DiceController(Automaty db, IBalanceService balanceService, DiceService diceService)
    {
        _db = db;
        _balanceService = balanceService;
        _diceService = diceService;
    }

    [HttpPost("roll")]
    public async Task<IActionResult> Roll([FromBody] DiceRollRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (request.Bet <= 0)
            return BadRequest(new { error = "Stawka musi byc wieksza od zera." });

        if (request.Bet > 100000)
            return BadRequest(new { error = "Maksymalna stawka w Dice to 100000." });

        if (!DiceService.IsValidTarget(request.Target))
            return BadRequest(new { error = "Prog Dice musi byc w zakresie 2-98." });

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var betResult = await _balanceService.PlaceBetAsync(userId.Value, request.Bet, gameName: "Dice");
            if (!betResult.Success)
                return BadRequest(new { error = betResult.Error, balance = betResult.Balance });

            var result = _diceService.Play(request.Bet, request.Mode, request.Target);
            _db.DiceGames.Add(new DiceGame
            {
                UserId = userId.Value,
                BetAmount = request.Bet,
                Mode = result.Mode,
                Target = result.Target,
                Roll = result.Roll,
                Multiplier = result.Multiplier,
                WinAmount = result.Win
            });
            await _db.SaveChangesAsync();

            var payoutResult = await _balanceService.PayoutAsync(userId.Value, result.Win);
            if (!payoutResult.Success)
                return BadRequest(new { error = payoutResult.Error });

            await transaction.CommitAsync();

            return Ok(new
            {
                mode = result.Mode,
                target = result.Target,
                roll = result.Roll,
                chance = result.Chance,
                multiplier = result.Multiplier,
                win = result.Win,
                balance = payoutResult.Balance
            });
        }, null, CancellationToken.None);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

public record DiceRollRequest(decimal Bet, string Mode, int Target);

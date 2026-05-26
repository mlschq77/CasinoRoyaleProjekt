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
[Route("api/slot")]
public class SlotController : ControllerBase
{
    private readonly Automaty _db;
    private readonly IBalanceService _balanceService;
    private readonly SlotService _slotService;

    public SlotController(Automaty db, IBalanceService balanceService, SlotService slotService)
    {
        _db = db;
        _balanceService = balanceService;
        _slotService = slotService;
    }

    [HttpPost("spin")]
    public async Task<IActionResult> Spin([FromBody] SlotSpinRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (request.Bet <= 0)
            return BadRequest(new { error = "Stawka musi byc wieksza od zera." });

        if (request.Bet > 100000)
            return BadRequest(new { error = "Maksymalna stawka w slocie to 100000." });

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var betResult = await _balanceService.PlaceBetAsync(userId.Value, request.Bet);
            if (!betResult.Success)
                return BadRequest(new { error = betResult.Error, balance = betResult.Balance });

            var result = _slotService.Play(request.Bet);
            _db.SlotGames.Add(new SlotGame
            {
                UserId = userId.Value,
                BetAmount = request.Bet,
                SymbolsJson = JsonSerializer.Serialize(result.Reels.Select(symbol => symbol.Id)),
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
                reels = result.Reels.Select(symbol => new
                {
                    id = symbol.Id,
                    name = symbol.Name,
                    icon = symbol.Icon
                }),
                multiplier = result.Multiplier,
                win = result.Win,
                balance = payoutResult.Balance
            });
        }, null, CancellationToken.None);
    }

    [AllowAnonymous]
    [HttpGet("paytable")]
    public IActionResult PayTable()
    {
        return Ok(new
        {
            symbols = _slotService.GetPayTable().Select(symbol => new
            {
                id = symbol.Id,
                name = symbol.Name,
                icon = symbol.Icon,
                tripleMultiplier = symbol.TripleMultiplier
            }),
            pairMultiplier = 0.50m,
            cherryPairMultiplier = 1.50m
        });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

public record SlotSpinRequest(decimal Bet);

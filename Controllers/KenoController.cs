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
[Route("api/keno")]
public class KenoController : ControllerBase
{
    private readonly Automaty _db;
    private readonly IBalanceService _balanceService;
    private readonly KenoService _kenoService;

    public KenoController(Automaty db, IBalanceService balanceService, KenoService kenoService)
    {
        _db = db;
        _balanceService = balanceService;
        _kenoService = kenoService;
    }

    [HttpPost("draw")]
    public async Task<IActionResult> Draw([FromBody] KenoDrawRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (request.Bet <= 0)
            return BadRequest(new { error = "Stawka musi byc wieksza od zera." });

        if (request.Bet > 100000)
            return BadRequest(new { error = "Maksymalna stawka w Keno to 100000." });

        var selected = KenoService.NormalizeNumbers(request.Numbers ?? []);
        if (!KenoService.IsValidSelection(selected))
            return BadRequest(new { error = "Wybierz od 1 do 10 liczb z zakresu 1-40." });

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var betResult = await _balanceService.PlaceBetAsync(userId.Value, request.Bet, gameName: "Keno");
            if (!betResult.Success)
                return BadRequest(new { error = betResult.Error, balance = betResult.Balance });

            var result = _kenoService.Play(request.Bet, selected);
            _db.KenoGames.Add(new KenoGame
            {
                UserId = userId.Value,
                BetAmount = request.Bet,
                SelectedNumbers = JsonSerializer.Serialize(result.SelectedNumbers),
                DrawnNumbers = JsonSerializer.Serialize(result.DrawnNumbers),
                Hits = result.Hits,
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
                selectedNumbers = result.SelectedNumbers,
                drawnNumbers = result.DrawnNumbers,
                hits = result.Hits,
                multiplier = result.Multiplier,
                win = result.Win,
                balance = payoutResult.Balance
            });
        }, null, CancellationToken.None);
    }

    [AllowAnonymous]
    [HttpGet("paytable")]
    public IActionResult PayTable(int picked = 10)
    {
        return Ok(new { multipliers = _kenoService.GetPayTable(Math.Clamp(picked, 1, 10)) });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

public record KenoDrawRequest(decimal Bet, List<int>? Numbers);

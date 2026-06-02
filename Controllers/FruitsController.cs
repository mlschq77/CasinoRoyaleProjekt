using CasinoRoyale.Data;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
[ApiController]
[Route("api/fruits")]
public class FruitsController : ControllerBase
{
    private readonly Automaty _db;
    private readonly IBalanceService _balanceService;
    private readonly FruitsService _fruitsService;

    public FruitsController(Automaty db, IBalanceService balanceService, FruitsService fruitsService)
    {
        _db = db;
        _balanceService = balanceService;
        _fruitsService = fruitsService;
    }

    [HttpPost("spin")]
    public async Task<IActionResult> Spin(decimal bet)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        if (bet <= 0)
            return BadRequest(new { error = "Stawka musi byc wieksza od zera." });

        if (bet > 100000)
            return BadRequest(new { error = "Maksymalna stawka na Fruits to 100000." });

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var betResult = await _balanceService.PlaceBetAsync(userId.Value, bet, gameName: "Fruits");
            if (!betResult.Success)
                return BadRequest(new { error = betResult.Error, balance = betResult.Balance });

            var result = _fruitsService.Spin(bet);
            var payoutResult = await _balanceService.PayoutAsync(userId.Value, result.Win, betResult.SessionKey);

            if (!payoutResult.Success)
                return BadRequest(new { error = payoutResult.Error });

            await transaction.CommitAsync();

            return Ok(new
            {
                reels = result.Reels.Select(symbol => new
                {
                    symbol.Id,
                    symbol.Icon,
                    symbol.Name
                }),
                multiplier = result.Multiplier,
                win = result.Win,
                result.Message,
                balance = payoutResult.Balance,
                balanceBonus = payoutResult.BalanceBonus
            });
        }, null, CancellationToken.None);
    }

    [AllowAnonymous]
    [HttpGet("paytable")]
    public IActionResult Paytable()
    {
        return Ok(new { payouts = _fruitsService.GetPaytable() });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

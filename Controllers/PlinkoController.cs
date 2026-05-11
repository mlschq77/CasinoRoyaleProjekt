using CasinoRoyale.Data;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
[ApiController]
[Route("api/plinko")]
public class PlinkoController : ControllerBase
{
    private readonly Automaty _context;
    private readonly IBalanceService _balanceService;
    private readonly PlinkoService _plinkoService;

    public PlinkoController(Automaty context, IBalanceService balanceService, PlinkoService plinkoService)
    {
        _context = context;
        _balanceService = balanceService;
        _plinkoService = plinkoService;
    }

    [HttpPost("play")]
    public async Task<IActionResult> Play(decimal bet, string risk = "medium")
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        if (bet <= 0)
            return BadRequest(new { error = "Stawka musi byc wieksza od zera." });

        if (bet > 100000)
            return BadRequest(new { error = "Maksymalna stawka w Plinko to 100000." });

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var betResult = await _balanceService.PlaceBetAsync(userId.Value, bet);
            if (!betResult.Success)
                return BadRequest(new { error = betResult.Error, balance = betResult.Balance });

            var result = _plinkoService.Play(bet, risk);
            var payoutResult = await _balanceService.PayoutAsync(userId.Value, result.Win);

            if (!payoutResult.Success)
                return BadRequest(new { error = payoutResult.Error });

            await transaction.CommitAsync();

            return Ok(new
            {
                risk = result.Risk,
                path = result.Path.Select(step => step == PlinkoStep.Right ? "R" : "L"),
                landingSlot = result.LandingSlot,
                multiplier = result.Multiplier,
                win = result.Win,
                balance = payoutResult.Balance
            });
        }, null, CancellationToken.None);
    }

    [AllowAnonymous]
    [HttpGet("multipliers")]
    public IActionResult Multipliers(string risk = "medium")
    {
        return Ok(new { multipliers = _plinkoService.GetMultipliers(risk) });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

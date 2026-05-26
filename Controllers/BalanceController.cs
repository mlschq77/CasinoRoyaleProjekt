using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
[ApiController]
[Route("api/balance")]
public class BalanceController : ControllerBase
{
    private readonly IBalanceService _balanceService;

    public BalanceController(IBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    [HttpGet]
    public async Task<IActionResult> Current()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var info = await _balanceService.GetBalanceInfoAsync(userId.Value);
        if (info == null)
            return NotFound(new { error = "Nie znaleziono uzytkownika." });

        return Ok(new
        {
            balance = info.TotalBalance,
            balanceReal = info.BalanceReal,
            balanceBonus = info.BalanceBonus,
            hasActiveBonus = info.HasActiveBonus,
            wageringProgress = info.WageringProgress,
            wageringRequired = info.WageringRequired,
            expiresAt = info.ExpiresAt
        });
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}
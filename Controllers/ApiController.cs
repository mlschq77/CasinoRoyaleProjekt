using CasinoRoyale.Data;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static CasinoRoyale.Services.PaginationHelper;

namespace CasinoRoyale.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ApiController : ControllerBase
{
    private readonly Automaty _db;

    public ApiController(Automaty db)
    {
        _db = db;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("bet-history")]
    public async Task<ActionResult<PaginatedResult<BetHistoryDto>>> GetBetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? gameName = null)
    {
        var userId = GetUserId();

        var query = _db.BetRecords
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Settled);

        if (!string.IsNullOrWhiteSpace(gameName))
            query = query.Where(r => r.GameName == gameName);

        query = query.OrderByDescending(r => r.CreatedAt);

        var result = await PaginateAsync(query, page, pageSize);

        var dtos = result.Items.Select(r => new BetHistoryDto
        {
            Id = r.Id,
            GameName = r.GameName,
            Amount = r.Amount,
            PayoutAmount = r.PayoutAmount,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new PaginatedResult<BetHistoryDto>(dtos, result.TotalCount, result.Page, result.PageSize);
    }

    [HttpGet("game-stats")]
    public async Task<ActionResult<List<GameStatsDto>>> GetGameStats()
    {
        var userId = GetUserId();

        var stats = await _db.BetRecords
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Settled)
            .GroupBy(r => r.GameName)
            .Select(g => new GameStatsDto
            {
                GameName = g.Key,
                TotalBets = g.Count(),
                TotalAmount = g.Sum(r => r.Amount),
                TotalPayout = g.Sum(r => r.PayoutAmount ?? 0),
                RtpPercent = g.Sum(r => r.PayoutAmount ?? 0) * 100 / g.Sum(r => r.Amount)
            })
            .ToListAsync();

        return Ok(stats);
    }

    [HttpGet("balance")]
    public async Task<ActionResult<BalanceDto>> GetBalance()
    {
        var userId = GetUserId();

        var wallet = await _db.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null) return NotFound();

        return Ok(new BalanceDto
        {
            BalanceReal = wallet.BalanceReal,
            BalanceBonus = wallet.BalanceBonus,
            WageringProgress = wallet.WageringProgress ?? 0,
            WageringRequired = wallet.WageringRequired ?? 0
        });
    }
}

public class BetHistoryDto
{
    public int Id { get; set; }
    public string GameName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? PayoutAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GameStatsDto
{
    public string GameName { get; set; } = string.Empty;
    public int TotalBets { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalPayout { get; set; }
    public decimal RtpPercent { get; set; }
}

public class BalanceDto
{
    public decimal BalanceReal { get; set; }
    public decimal BalanceBonus { get; set; }
    public decimal WageringProgress { get; set; }
    public decimal WageringRequired { get; set; }
}

using CasinoRoyale.Data;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static CasinoRoyale.Services.PaginationHelper;

namespace CasinoRoyale.Controllers;

/// <summary>
/// REST API dla zalogowanych użytkowników. Wymaga autoryzacji ciasteczkowej.
/// </summary>
[Authorize]
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ApiController : ControllerBase
{
    private readonly Automaty _db;

    public ApiController(Automaty db)
    {
        _db = db;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Historia zakładów z paginacją.
    /// </summary>
    /// <param name="page">Numer strony (1-based)</param>
    /// <param name="pageSize">Rozmiar strony (max 100)</param>
    /// <param name="gameName">Filtrowanie po nazwie gry (opcjonalne)</param>
    /// <returns>PaginatedResult z listą zakładów</returns>
    /// <response code="200">Zwraca stronicowaną historię zakładów</response>
    /// <response code="401">Brak autoryzacji</response>
    [HttpGet("bet-history")]
    [ProducesResponseType(typeof(PaginatedResult<BetHistoryDto>), 200)]
    public async Task<IActionResult> GetBetHistory(
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

        return Ok(new PaginatedResult<BetHistoryDto>(dtos, result.TotalCount, result.Page, result.PageSize));
    }

    /// <summary>
    /// Statystyki gier: liczba zakładów, suma stawek, suma wygranych, RTP%.
    /// </summary>
    [HttpGet("game-stats")]
    [ProducesResponseType(typeof(List<GameStatsDto>), 200)]
    public async Task<IActionResult> GetGameStats()
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

    /// <summary>
    /// Aktualny stan konta: saldo real, bonus, wagering progress.
    /// </summary>
    [HttpGet("balance")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> GetBalance()
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

/// <summary>Szczegóły pojedynczego zakładu.</summary>
public class BetHistoryDto
{
    /// <summary>Identyfikator zakładu.</summary>
    public int Id { get; set; }
    /// <summary>Nazwa gry (np. "Mines", "Blackjack").</summary>
    public string GameName { get; set; } = string.Empty;
    /// <summary>Kwota zakładu.</summary>
    public decimal Amount { get; set; }
    /// <summary>Kwota wypłaty (null jeśli nierozliczony).</summary>
    public decimal? PayoutAmount { get; set; }
    /// <summary>Data utworzenia zakładu (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Statystyki dla jednej gry.</summary>
public class GameStatsDto
{
    /// <summary>Nazwa gry.</summary>
    public string GameName { get; set; } = string.Empty;
    /// <summary>Liczba zakładów.</summary>
    public int TotalBets { get; set; }
    /// <summary>Suma postawionych kwot.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Suma wypłat.</summary>
    public decimal TotalPayout { get; set; }
    /// <summary>Procent zwrotu (RTP).</summary>
    public decimal RtpPercent { get; set; }
}

/// <summary>Stan konta użytkownika.</summary>
public class BalanceDto
{
    /// <summary>Saldo prawdziwych środków.</summary>
    public decimal BalanceReal { get; set; }
    /// <summary>Saldo bonusowe.</summary>
    public decimal BalanceBonus { get; set; }
    /// <summary>Postęp wageringu.</summary>
    public decimal WageringProgress { get; set; }
    /// <summary>Wymagany wagering.</summary>
    public decimal WageringRequired { get; set; }
}

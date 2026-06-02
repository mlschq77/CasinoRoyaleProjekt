using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoRoyale.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/crash")]
    public class CrashController : ControllerBase
    {
        private readonly Automaty _context;
        private readonly CrashService _service;
        private readonly IBalanceService _balanceService;

        public CrashController(Automaty context, CrashService service, IBalanceService balanceService)
        {
            _context = context;
            _service = service;
            _balanceService = balanceService;
        }

        /// <summary>
        /// 1. Rozpoczęcie gry - losuje crash point, zapisuje w bazie, zwraca tylko startTime (bez crash pointu!)
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartGame(decimal bet)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized();

                if (bet <= 0)
                    return BadRequest(new { error = "Stawka musi byc wieksza od zera." });

                var strategy = _context.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    var crashPoint = _service.GenerateCrashPoint();

                    var session = new CrashSession
                    {
                        UserId = userId.Value,
                        BetAmount = bet,
                        CrashPoint = crashPoint,
                        StartTime = DateTime.UtcNow,
                        IsActive = true
                    };

                    _context.CrashSessions.Add(session);
                    await _context.SaveChangesAsync();

                    var sessionKey = "csh:" + session.Id;
                    var betResult = await _balanceService.PlaceBetAsync(userId.Value, bet, sessionKey, gameName: "Crash");
                    if (!betResult.Success)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { error = betResult.Error, balance = betResult.Balance });
                    }

                    await transaction.CommitAsync();

                    // NIGDY nie wysyłamy crashPoint do klienta!
                    return Ok(new
                    {
                        success = true,
                        sessionId = session.Id,
                        startTime = session.StartTime,
                        balance = betResult.Balance,
                        balanceBonus = betResult.BalanceBonus
                    });
                }, null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 2. Kliknięcie "Wypłać" - używa mnożnika przesłanego przez klienta (z walidacją serwerową),
        ///    aby wypłata była zgodna z tym co gracz widział na ekranie.
        /// </summary>
        [HttpPost("cashout")]
        public async Task<IActionResult> Cashout(int sessionId, decimal? clientMultiplier = null)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var session = await _context.CrashSessions.FindAsync(sessionId);
                if (session == null || session.UserId != userId.Value || !session.IsActive)
                    return BadRequest(new { error = "Sesja nieaktywna lub nie znaleziona." });

                // Obliczamy ile czasu minęło NA SERWERZE
                var timeElapsed = DateTime.UtcNow - session.StartTime;
                var serverMultiplier = _service.CalculateMultiplier(timeElapsed);

                // Używamy mnożnika przesłanego przez klienta (dokładnie to co gracz widział na ekranie).
                // Walidacja: mnożnik musi być >= 1.0 i nie może wyprzedzać serwera o więcej niż tolerancję
                // (zapobiega oszustwom typu "skok w przyszłość").
                // Zabezpieczeniem przed oddaniem zbyt dużego jest CrashPoint — jeśli clientMultiplier
                // przekracza CrashPoint, gracz przegrywa.
                const decimal tolerance = 0.1m; // ~160ms rozbieżności zegarów

                decimal effectiveMultiplier;
                if (clientMultiplier.HasValue
                    && clientMultiplier.Value >= 1.0m
                    && clientMultiplier.Value <= serverMultiplier + tolerance)
                {
                    // Używamy dokładnie tego co gracz widział na ekranie
                    effectiveMultiplier = clientMultiplier.Value;
                }
                else
                {
                    // Fallback: serwer decyduje (np. przy próbie nadużycia)
                    effectiveMultiplier = serverMultiplier;
                }

                session.IsActive = false;

                if (effectiveMultiplier <= session.CrashPoint)
                {
                    // Gracz zdążył - wygrywa!
                    var winAmount = _service.CalculateWin(session.BetAmount, effectiveMultiplier);

                    session.CashoutMultiplier = effectiveMultiplier;
                    session.WinAmount = winAmount;

                    await _context.SaveChangesAsync();

                    var sessionKey = "csh:" + session.Id;
                    var payoutResult = await _balanceService.PayoutAsync(userId.Value, winAmount, sessionKey);
                    if (!payoutResult.Success)
                        return BadRequest(new { error = payoutResult.Error });

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        success = true,
                        won = true,
                        multiplier = effectiveMultiplier,
                        winAmount,
                        crashPoint = session.CrashPoint,
                        balance = payoutResult.Balance,
                        balanceBonus = payoutResult.BalanceBonus
                    });
                }
                else if (clientMultiplier.HasValue
                         && clientMultiplier.Value >= 1.0m
                         && clientMultiplier.Value <= session.CrashPoint)
                {
                    // Crash już nastąpił na serwerze, ale gracz próbował wypłacić PRZED crashpointem.
                    // Nie wiedział o crashu, bo polling (250ms) nie zdążył poinformować klienta.
                    // Honorujemy wypłatę — gracz jest fair.
                    effectiveMultiplier = clientMultiplier.Value;
                    var winAmount = _service.CalculateWin(session.BetAmount, effectiveMultiplier);

                    session.CashoutMultiplier = effectiveMultiplier;
                    session.WinAmount = winAmount;

                    await _context.SaveChangesAsync();

                    var sessionKey = "csh:" + session.Id;
                    var payoutResult = await _balanceService.PayoutAsync(userId.Value, winAmount, sessionKey);
                    if (!payoutResult.Success)
                        return BadRequest(new { error = payoutResult.Error });

                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        success = true,
                        won = true,
                        multiplier = effectiveMultiplier,
                        winAmount,
                        crashPoint = session.CrashPoint,
                        balance = payoutResult.Balance,
                        balanceBonus = payoutResult.BalanceBonus
                    });
                }
                else
                {
                    // Gracz spóźnił się - CrashPoint został już przekroczony
                    var sessionKey = "csh:" + session.Id;
                    await _balanceService.PayoutAsync(userId.Value, 0, sessionKey);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    var balanceInfo = await _balanceService.GetBalanceInfoAsync(userId.Value);

                    return Ok(new
                    {
                        success = true,
                        won = false,
                        crashPoint = session.CrashPoint,
                        balance = balanceInfo?.BalanceReal + balanceInfo?.BalanceBonus,
                        balanceBonus = balanceInfo?.BalanceBonus
                    });
                }
            }, null, CancellationToken.None);
        }

        /// <summary>
        /// 3. Sprawdzanie statusu (Polling) - czy gra już "wybuchła"
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(int sessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var session = await _context.CrashSessions.FindAsync(sessionId);
            if (session == null || session.UserId != userId.Value)
                return NotFound(new { error = "Sesja nie znaleziona." });

            var timeElapsed = DateTime.UtcNow - session.StartTime;
            var currentMultiplier = _service.CalculateMultiplier(timeElapsed);

            if (currentMultiplier > session.CrashPoint)
            {
                // Tylko informujemy klienta — NIE zmieniamy IsActive!
                // (zapobiega race condition z równoczesnym cashout)
                return Ok(new
                {
                    crashed = true,
                    crashPoint = session.CrashPoint
                });
            }

            return Ok(new { crashed = false });
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CasinoRoyale.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/mines")]
    public class MinesController : ControllerBase
    {
        private readonly Automaty _context;
        private readonly MinesService _service;
        private readonly IBalanceService _balanceService;


        public MinesController(Automaty context, MinesService service, IBalanceService balanceService)
        {
            _context = context;
            _service = service;
            _balanceService = balanceService;
        }



        [HttpPost("start")]
        public async Task<IActionResult> StartGame(int mineCount, decimal bet)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                    return Unauthorized();

                if (mineCount < 1 || mineCount >= 25)
                    return BadRequest(new { error = "Liczba min musi byc w zakresie 1-24." });

                var strategy = _context.Database.CreateExecutionStrategy();

                return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
                {
                    await using var transaction = await _context.Database.BeginTransactionAsync();

                    var mines = _service.GenerateMines(mineCount);

                    var game = new MinesGame
                    {
                        UserId = userId.Value,
                        MineCount = mineCount,
                        MinePositions = string.Join(",", mines),
                        BetAmount = bet,
                        IsActive = true
                    };

                    _context.MinesGames.Add(game);
                    await _context.SaveChangesAsync();

                    var sessionKey = "min:" + game.Id;
                    var betResult = await _balanceService.PlaceBetAsync(userId.Value, bet, sessionKey, gameName: "Mines");
                    if (!betResult.Success)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { error = betResult.Error, balance = betResult.Balance });
                    }

                    await transaction.CommitAsync();

                    return Ok(new { id = game.Id, balance = betResult.Balance, balanceBonus = betResult.BalanceBonus });
                }, null, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }



        [HttpPost("click")]
        public async Task<IActionResult> Click(int gameId, int position)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var game = await _context.MinesGames.FindAsync(gameId);

            if (game == null || game.UserId != userId.Value || !game.IsActive)
                return BadRequest(new { error = "Game ended" });

            var mines = game.MinePositions.Split(',').Select(int.Parse).ToList();
            var revealed = string.IsNullOrEmpty(game.RevealedPositions)
                ? new List<int>()
                : game.RevealedPositions.Split(',').Select(int.Parse).ToList();

            if (revealed.Contains(position))
                return BadRequest(new { error = "Already clicked" });

            if (mines.Contains(position))
            {
                game.IsActive = false;
                await _context.SaveChangesAsync();

                var sessionKey = "min:" + game.Id;
                var payoutResult = await _balanceService.PayoutAsync(userId.Value, 0, sessionKey);

                return Ok(new
                {
                    result = "lose",
                    balance = payoutResult.Balance,
                    balanceBonus = payoutResult.BalanceBonus
                });
            }

            revealed.Add(position);
            game.RevealedPositions = string.Join(",", revealed);

            await _context.SaveChangesAsync();

            return Ok(new { result = "safe" });
        }

        [HttpPost("cashout")]
        public async Task<IActionResult> Cashout(int gameId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync<object, IActionResult>(null!, async (_, _, _) =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                var game = await _context.MinesGames.FindAsync(gameId);

                if (game == null || game.UserId != userId.Value || !game.IsActive)
                    return BadRequest(new { error = "Game ended" });

                int revealedCount = string.IsNullOrEmpty(game.RevealedPositions)
                    ? 0
                    : game.RevealedPositions.Split(',').Length;

                var multiplier = _service.CalculateMultiplier(revealedCount);
                var win = game.BetAmount * multiplier;

                game.IsActive = false;
                await _context.SaveChangesAsync();

                var sessionKey = "min:" + game.Id;
                var payoutResult = await _balanceService.PayoutAsync(userId.Value, win, sessionKey);
                if (!payoutResult.Success)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { error = payoutResult.Error });
                }

                await transaction.CommitAsync();

                return Ok(new { win, multiplier, balance = payoutResult.Balance, balanceBonus = payoutResult.BalanceBonus });
            }, null, CancellationToken.None);
        }

        [HttpGet("reveal")]
        public IActionResult Reveal(int gameId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var game = _context.MinesGames.Find(gameId);

            if (game == null || game.UserId != userId.Value)
                return BadRequest(new { error = "Game not found" });

            var mines = game.MinePositions
                .Split(',')
                .Select(int.Parse)
                .ToList();

            return Ok(new { mines });
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }

    }

}
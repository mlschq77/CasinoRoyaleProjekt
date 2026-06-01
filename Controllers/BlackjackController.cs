using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoRoyale.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/blackjack")]
    public class BlackjackController : ControllerBase
    {
        private readonly BlackjackService _service;
        private readonly IBalanceService _balanceService;

        public BlackjackController(BlackjackService service, IBalanceService balanceService)
        {
            _service = service;
            _balanceService = balanceService;
        }

        private async Task<decimal?> GetBalanceBonusAsync(int userId)
        {
            var info = await _balanceService.GetBalanceInfoAsync(userId);
            return info?.BalanceBonus;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.StartGameAsync(userId.Value, req.Bet);
            if (!success) return BadRequest(new { error, balance });

            var state = _service.BuildGameState(game!);
            var bonus = await GetBalanceBonusAsync(userId.Value);
            return Ok(new { state, balance, balanceBonus = bonus });
        }

        [HttpPost("hit")]
        public async Task<IActionResult> Hit([FromBody] GameActionRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.HitAsync(userId.Value, req.GameId);
            if (!success) return BadRequest(new { error, balance });

            var state = _service.BuildGameState(game!);
            var bonus = await GetBalanceBonusAsync(userId.Value);
            return Ok(new { state, balance, balanceBonus = bonus });
        }

        [HttpPost("stand")]
        public async Task<IActionResult> Stand([FromBody] GameActionRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.StandAsync(userId.Value, req.GameId);
            if (!success) return BadRequest(new { error });

            var state = _service.BuildGameState(game!);
            var bonus = await GetBalanceBonusAsync(userId.Value);
            return Ok(new { state, balance, balanceBonus = bonus });
        }

        [HttpPost("double")]
        public async Task<IActionResult> Double([FromBody] GameActionRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.DoubleDownAsync(userId.Value, req.GameId, req.FaceDown);
            if (!success) return BadRequest(new { error, balance });

            var state = _service.BuildGameState(game!);
            var bonus = await GetBalanceBonusAsync(userId.Value);
            return Ok(new { state, balance, balanceBonus = bonus });
        }

        [HttpPost("split")]
        public async Task<IActionResult> Split([FromBody] GameActionRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.SplitAsync(userId.Value, req.GameId);
            if (!success) return BadRequest(new { error });

            var state = _service.BuildGameState(game!);
            var bonus = await GetBalanceBonusAsync(userId.Value);
            return Ok(new { state, balance, balanceBonus = bonus });
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var info = await _balanceService.GetBalanceInfoAsync(userId.Value);
            var balanceReal = info?.BalanceReal ?? 0m;
            var balanceBonus = info?.BalanceBonus ?? 0m;
            return Ok(new { balance = balanceReal + balanceBonus, balanceBonus });
        }

        private int? GetUserId()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(val, out var id) ? id : null;
        }
    }

    public class StartRequest
    {
        public decimal Bet { get; set; }
    }

    public class GameActionRequest
    {
        public int GameId { get; set; }
        public bool FaceDown { get; set; }
    }
}

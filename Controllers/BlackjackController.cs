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

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.StartGameAsync(userId.Value, req.Bet);
            if (!success) return BadRequest(new { error, balance });

            var state = _service.BuildGameState(game!);
            return Ok(new { state, balance });
        }

        [HttpPost("hit")]
        public async Task<IActionResult> Hit([FromBody] GameActionRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game) = await _service.HitAsync(userId.Value, req.GameId);
            if (!success) return BadRequest(new { error });

            decimal balance = 0;
            if (!game!.IsActive || (!game.IsPlayerTurn && !game.PayoutProcessed))
            {
                var bal = await _balanceService.GetBalanceAsync(userId.Value);
                balance = bal ?? 0;
            }

            var state = _service.BuildGameState(game!);
            return Ok(new { state, balance });
        }

        [HttpPost("stand")]
        public async Task<IActionResult> Stand([FromBody] GameActionRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.StandAsync(userId.Value, req.GameId);
            if (!success) return BadRequest(new { error });

            var state = _service.BuildGameState(game!);
            return Ok(new { state, balance });
        }

        [HttpPost("double")]
        public async Task<IActionResult> Double([FromBody] GameActionRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.DoubleDownAsync(userId.Value, req.GameId);
            if (!success) return BadRequest(new { error });

            var state = _service.BuildGameState(game!);
            return Ok(new { state, balance });
        }

        [HttpPost("split")]
        public async Task<IActionResult> Split([FromBody] GameActionRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.SplitAsync(userId.Value, req.GameId);
            if (!success) return BadRequest(new { error });

            var state = _service.BuildGameState(game!);
            return Ok(new { state, balance });
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var balance = await _balanceService.GetBalanceAsync(userId.Value);
            return Ok(new { balance });
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
    }
}

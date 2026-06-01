using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CasinoRoyale.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/baccarat")]
    public class BaccaratController : ControllerBase
    {
        private readonly BaccaratService _service;
        private readonly IBalanceService _balanceService;

        public BaccaratController(BaccaratService service, IBalanceService balanceService)
        {
            _service = service;
            _balanceService = balanceService;
        }

        private async Task<decimal?> GetBalanceBonusAsync(int userId)
        {
            var info = await _balanceService.GetBalanceInfoAsync(userId);
            return info?.BalanceBonus;
        }

        [HttpPost("play")]
        public async Task<IActionResult> Play([FromBody] BaccaratPlayRequest req)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var (success, error, game, balance) = await _service.PlayRoundAsync(userId.Value, req.Bet, req.BetType);
            if (!success) return BadRequest(new { error, balance });

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

    public class BaccaratPlayRequest
    {
        public decimal Bet { get; set; }
        public string BetType { get; set; } = "player";
    }
}

using Microsoft.AspNetCore.Mvc;
using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;

namespace CasinoRoyale.Controllers
{

    [ApiController]
    [Route("api/mines")]
    public class MinesController : ControllerBase
    {
        private readonly Automaty _context;
        private readonly MinesService _service;


        public MinesController(Automaty context, MinesService service)
        {
            _context = context;
            _service = service;
        }



        [HttpPost("start")]
        public IActionResult StartGame(int mineCount, decimal bet)
        {
            try
            {
                var mines = _service.GenerateMines(mineCount);

                var game = new MinesGame
                {
                    UserId = 1,
                    MineCount = mineCount,
                    MinePositions = string.Join(",", mines),
                    BetAmount = bet,
                    IsActive = true
                };

                _context.MinesGames.Add(game);
                _context.SaveChanges();

                return Ok(new { id = game.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }



        [HttpPost("click")]
        public IActionResult Click(int gameId, int position)
        {
            var game = _context.MinesGames.Find(gameId);

            if (game == null || !game.IsActive)
                return BadRequest("Game ended");

            var mines = game.MinePositions.Split(',').Select(int.Parse).ToList();
            var revealed = string.IsNullOrEmpty(game.RevealedPositions)
                ? new List<int>()
                : game.RevealedPositions.Split(',').Select(int.Parse).ToList();

            if (revealed.Contains(position))
                return BadRequest("Already clicked");

            if (mines.Contains(position))
            {
                game.IsActive = false;
                _context.SaveChanges();

                return Ok(new { result = "lose" });
            }

            revealed.Add(position);
            game.RevealedPositions = string.Join(",", revealed);

            _context.SaveChanges();

            return Ok(new { result = "safe" });
        }

        [HttpPost("cashout")]
        public IActionResult Cashout(int gameId)
        {
            var game = _context.MinesGames.Find(gameId);

            if (game == null || !game.IsActive)
                return BadRequest("Game ended");

            int revealedCount = string.IsNullOrEmpty(game.RevealedPositions)
                ? 0
                : game.RevealedPositions.Split(',').Length;

            var multiplier = _service.CalculateMultiplier(revealedCount);
            var win = game.BetAmount * multiplier;

            game.IsActive = false;
            _context.SaveChanges();

            return Ok(new { win, multiplier });
        }

        [HttpGet("reveal")]
        public IActionResult Reveal(int gameId)
        {
            var game = _context.MinesGames.Find(gameId);

            if (game == null)
                return BadRequest("Game not found");

            var mines = game.MinePositions
                .Split(',')
                .Select(int.Parse)
                .ToList();

            return Ok(new { mines });
        }

    }

}

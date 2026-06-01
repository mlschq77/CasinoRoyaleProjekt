using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasinoRoyale.Controllers;

[Authorize]
public class GamesController : Controller
{
    public IActionResult Mines()
    {
        return View();
    }

    public IActionResult Blackjack()
    {
        return View();
    }

    public IActionResult Plinko()
    {
        return View();
    }

    public IActionResult Crash()
    {
        return View();
    }

    public IActionResult Fruits()
    {
        return View();
    }

    public IActionResult Slot()
    {
        return RedirectToAction(nameof(Fruits));
    }

    public IActionResult Roulette()
    {
        return View();
    }
}

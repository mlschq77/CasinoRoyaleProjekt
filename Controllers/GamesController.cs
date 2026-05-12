using Microsoft.AspNetCore.Mvc;

namespace CasinoRoyale.Controllers;

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

    public IActionResult Slot()
    {
        return View();
    }
}
}
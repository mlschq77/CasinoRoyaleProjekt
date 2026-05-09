using Microsoft.AspNetCore.Mvc;

namespace CasinoRoyale.Controllers;

public class GamesController : Controller
{
    public IActionResult Mines()
    {
        return View();
    }
}

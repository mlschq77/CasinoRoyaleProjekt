using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CasinoRoyale.Controllers;

[Authorize]
public class GraController : Controller
{
    public IActionResult Graj(int id)
    {
        return id switch
        {
            0 => RedirectToAction("Mines", "Games"),
            1 => RedirectToAction("Blackjack", "Games"),
            2 => RedirectToAction("Plinko", "Games"),
            3 => RedirectToAction("Crash", "Games"),
            4 => RedirectToAction("Roulette", "Games"),
            5 => RedirectToAction("Dice", "Games"),
            6 => RedirectToAction("Keno", "Games"),
            7 => RedirectToAction("Baccarat", "Games"),
            _ => Content($"Gra ID: {id}")
        };
    }
}

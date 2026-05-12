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
            _ => Content($"Gra ID: {id}")
        };
    }
}
using Microsoft.AspNetCore.Mvc;

namespace CasinoRoyale.Controllers;

public class GraController : Controller
{
    public IActionResult Graj(int id)
    {
        return id switch
        {
            0 => RedirectToAction("Mines", "Games"),
            1 => RedirectToAction("Blackjack", "Games"),
            2 => RedirectToAction("Plinko", "Games"),
            _ => RedirectToAction("Oferta", "Automaty")
        };
    }
}
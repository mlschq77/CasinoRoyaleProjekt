using Microsoft.AspNetCore.Mvc;

namespace CasinoRoyale.Controllers;

public class GraController : Controller
{
    public IActionResult Graj(int id)
    {
        if (id == 1)
            return RedirectToAction("Mines", "Games");

        if (id == 2)
            return RedirectToAction("Plinko", "Games");

        return Content($"Gra ID: {id}");
    }
}

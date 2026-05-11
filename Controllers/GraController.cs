using Microsoft.AspNetCore.Mvc;

namespace CasinoRoyale.Controllers;

public class GraController : Controller
{
    public IActionResult Graj(int id)
    {
        if (id == 0)
            return RedirectToAction("Mines", "Games");

        return Content($"Gra ID: {id}");
    }
}

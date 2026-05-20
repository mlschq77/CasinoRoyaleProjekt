using System.Diagnostics;
using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly Automaty _dbContext;

    public HomeController(ILogger<HomeController> logger, Automaty dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult ProvablyFair()
    {
        return View();
    }

    public IActionResult Regulamin()
    {
        return View();
    }

    public IActionResult Kyc()
    {
        return View();
    }

    public async Task<IActionResult> Promocje()
    {
        var teraz = DateTime.UtcNow;
        var kody = await _dbContext.KodyBonusowe
            .AsNoTracking()
            .Where(k => k.WaznyDo == null || k.WaznyDo >= teraz)
            .OrderByDescending(k => k.Utworzono)
            .ToListAsync();

        return View(new PromocjeViewModel { Kody = kody });
    }

    public IActionResult Oferta()
    {
        return RedirectToAction("Oferta", "Automaty");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}

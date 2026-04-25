using System.Diagnostics;
using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Models.ViewModels;
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

    public async Task<IActionResult> Index()
    {
        var model = new HomeIndexViewModel
        {
            Gry = await _dbContext.AutomatyInfo
                .AsNoTracking()
                .Where(automat => automat.Rodzaj == "Automat")
                .OrderBy(automat => automat.Nazwa)
                .ToListAsync()
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

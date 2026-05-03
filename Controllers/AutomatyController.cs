using CasinoRoyale.Data;
using CasinoRoyale.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Controllers;

public class AutomatyController : Controller
{
    private readonly Automaty _dbContext;

    public AutomatyController(Automaty dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Oferta(string? kategoria)
    {
        var gryQuery = _dbContext.AutomatyInfo.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(kategoria))
        {
            gryQuery = gryQuery.Where(automat => automat.Kategoria == kategoria);
        }

        var model = new SlotsViewModel
        {
            Gry = await gryQuery
                .OrderBy(automat => automat.Nazwa)
                .ToListAsync(),
            Kategorie = await _dbContext.AutomatyInfo
                .AsNoTracking()
                .Select(automat => automat.Kategoria)
                .Where(kategoria => !string.IsNullOrWhiteSpace(kategoria))
                .Distinct()
                .OrderBy(kategoria => kategoria)
                .ToListAsync(),
            WybranaKategoria = kategoria
        };

        return View(model);
    }
}

using CasinoRoyale.Data;
using CasinoRoyale.Models;
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
        IQueryable<AutomatInfo> gryQuery = _dbContext.AutomatyInfo
            .AsNoTracking()
            .Include(automat => automat.Kategorie)
            .Include(automat => automat.Provider);

        if (!string.IsNullOrWhiteSpace(kategoria))
        {
            gryQuery = gryQuery.Where(automat => automat.Kategorie.Any(k => k.Nazwa == kategoria));
        }

        var gry = await gryQuery
            .OrderBy(automat => automat.Nazwa)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(kategoria) || kategoria == "Originals")
        {
            var provider = new AutomatProvider { Id = 0, Nazwa = "Casino Royale" };
            var originalsCat = new List<Kategoria> { new Kategoria { Id = 0, Nazwa = "Originals" } };

            gry.Add(new AutomatInfo
            {
                Id = 1,
                Nazwa = "Blackjack",
                Provider = provider,
                Kategorie = originalsCat
            });

            gry.Add(new AutomatInfo
            {
                Id = 0,
                Nazwa = "Mines",
                Provider = provider,
                Kategorie = originalsCat
            });

            gry.Add(new AutomatInfo
            {
                Id = 2,
                Nazwa = "Plinko",
                Provider = provider,
                Kategorie = originalsCat
            });

            gry.Add(new AutomatInfo
            {
                Id = 3,
                Nazwa = "Crash",
                Provider = provider,
                Kategorie = originalsCat
            });

        }

        var model = new SlotsViewModel
        {
            Gry = gry,
            Kategorie = (await _dbContext.Kategorie
                .AsNoTracking()
                .Select(k => k.Nazwa)
                .Where(nazwa => !string.IsNullOrWhiteSpace(nazwa))
                .OrderBy(k => k)
                .ToListAsync())
                .Append("Originals")
                .Distinct()
                .ToList(),

            WybranaKategoria = kategoria
        };

        return View(model);
    }
}
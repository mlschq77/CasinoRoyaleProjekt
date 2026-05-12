using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Controllers;

public class AutomatyController : Controller
{
    private readonly Automaty _dbContext;


    public class GraController : Controller
    {
        public IActionResult Graj(int id)
        {
            
            if (id == 1)
            {
                return Redirect("Home");
            }

            
            return Content($"Gra ID: {id}");
        }
    }


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

        if (string.IsNullOrWhiteSpace(kategoria) || kategoria == "Originals") // to trzeba przeniesc do bazy zamiast tutaj dodawac
        {
            gry.Add(new CasinoRoyale.Models.AutomatInfo
            {
                Id = 0,
                Nazwa = "Mines",
                Provider = new CasinoRoyale.Models.AutomatProvider
                {
                    Id = 0,
                    Nazwa = "Casino Royale"
                },
                Kategorie =
                [
                    new CasinoRoyale.Models.Kategoria
                    {
                        Id = 0,
                        Nazwa = "Originals"
                    }
                ]
            });

            gry.Add(new CasinoRoyale.Models.AutomatInfo
            {
                Id = 0,
                Nazwa = "Plinko",
                Provider = new CasinoRoyale.Models.AutomatProvider
                {
                    Id = 0,
                    Nazwa = "Casino Royale"
                },
                Kategorie =
                [
                    new CasinoRoyale.Models.Kategoria
                    {
                        Id = 2,
                        Nazwa = "Originals"
                    }
                ]
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

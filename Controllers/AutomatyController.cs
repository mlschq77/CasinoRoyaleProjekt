using CasinoRoyale.Data;
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
        var gryQuery = _dbContext.AutomatyInfo.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(kategoria))
        {
            gryQuery = gryQuery.Where(automat => automat.Kategoria == kategoria);
        }

        var gry = await gryQuery
            .OrderBy(automat => automat.Nazwa)
            .ToListAsync();

        if (string.IsNullOrWhiteSpace(kategoria) || kategoria == "Originals")
        {
            gry.Add(new CasinoRoyale.Models.AutomatInfo
            {
                Id = 1,
                Nazwa = "Mines",
                Kategoria = "Originals"
            });
        }

        var model = new SlotsViewModel
        {
            Gry = gry,
            Kategorie = (await _dbContext.AutomatyInfo
                .AsNoTracking()
                .Select(a => a.Kategoria)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct()
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

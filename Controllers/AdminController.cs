using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly Automaty _db;

    public AdminController(Automaty db)
    {
        _db = db;
    }

    // Sprawdza czy zalogowany uzytkownik to admin
    private bool IsAdmin()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        return email?.ToLower() == "admin@admin.pl";
    }

    // ─── DASHBOARD ───────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        if (!IsAdmin()) return Forbid();

        var vm = new AdminDashboardViewModel
        {
            TotalUsers      = await _db.Users.CountAsync(),
            TotalBalance    = await _db.Users.SumAsync(u => u.Balance),
            TotalDeposits   = await _db.StripePayments.SumAsync(p => (decimal?)p.Amount) ?? 0,
            TotalWithdrawals = await _db.StripeWithdrawals.SumAsync(w => (decimal?)w.Amount) ?? 0,
            RecentUsers     = await _db.Users
                                .OrderByDescending(u => u.DataRejestracji)
                                .Take(5)
                                .ToListAsync(),
            ActiveBonusCodes = await _db.KodyBonusowe
                                .Where(k => k.WaznyDo == null || k.WaznyDo > DateTime.UtcNow)
                                .CountAsync()
        };

        return View(vm);
    }

    // ─── UZYTKOWNICY ─────────────────────────────────────────────────────────

    public async Task<IActionResult> Uzytkownicy(string? search)
    {
        if (!IsAdmin()) return Forbid();

        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(s) ||
                u.Nazwa.ToLower().Contains(s) ||
                u.Imie.ToLower().Contains(s) ||
                u.Nazwisko.ToLower().Contains(s));
        }

        var users = await query.OrderByDescending(u => u.DataRejestracji).ToListAsync();
        ViewBag.Search = search;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EdytujSaldo(int userId, decimal newBalance)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Balance = Math.Max(0, newBalance);
        await _db.SaveChangesAsync();

        TempData["AdminMsg"] = $"Saldo uzytkownika {user.Email} zaktualizowane na {user.Balance:F2} PLN.";
        return RedirectToAction(nameof(Uzytkownicy));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsunUzytkownika(int userId)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Nie mozna usunac samego admina
        if (user.Email.ToLower() == "admin@admin.pl")
        {
            TempData["AdminMsg"] = "Nie mozna usunac konta admina.";
            return RedirectToAction(nameof(Uzytkownicy));
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        TempData["AdminMsg"] = $"Uzytkownik {user.Email} zostal usuniety.";
        return RedirectToAction(nameof(Uzytkownicy));
    }

    // ─── KODY BONUSOWE ───────────────────────────────────────────────────────

    public async Task<IActionResult> KodyBonusowe()
    {
        if (!IsAdmin()) return Forbid();

        var kody = await _db.KodyBonusowe
            .OrderByDescending(k => k.Utworzono)
            .ToListAsync();

        return View(kody);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DodajKod(KodBonusowy model)
    {
        if (!IsAdmin()) return Forbid();

        if (string.IsNullOrWhiteSpace(model.Kod))
        {
            TempData["AdminMsg"] = "Kod nie moze byc pusty.";
            return RedirectToAction(nameof(KodyBonusowe));
        }

        var exists = await _db.KodyBonusowe.AnyAsync(k => k.Kod == model.Kod);
        if (exists)
        {
            TempData["AdminMsg"] = $"Kod '{model.Kod}' juz istnieje.";
            return RedirectToAction(nameof(KodyBonusowe));
        }

        model.Utworzono = DateTime.UtcNow;
        _db.KodyBonusowe.Add(model);
        await _db.SaveChangesAsync();

        TempData["AdminMsg"] = $"Kod '{model.Kod}' zostal dodany.";
        return RedirectToAction(nameof(KodyBonusowe));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UsunKod(int id)
    {
        if (!IsAdmin()) return Forbid();

        var kod = await _db.KodyBonusowe.FindAsync(id);
        if (kod == null) return NotFound();

        _db.KodyBonusowe.Remove(kod);
        await _db.SaveChangesAsync();

        TempData["AdminMsg"] = $"Kod '{kod.Kod}' zostal usuniety.";
        return RedirectToAction(nameof(KodyBonusowe));
    }

    // ─── STATYSTYKI GIE ─────────────────────────────────────────────────────

    public async Task<IActionResult> Statystyki()
    {
        if (!IsAdmin()) return Forbid();

        var vm = new AdminStatystykiViewModel
        {
            LiczbaBlackjack = await _db.BlackjackGames.CountAsync(),
            LiczbaMines     = await _db.MinesGames.CountAsync(),
            LiczbaPlinko    = await _db.PlinkoGames.CountAsync(),
            SumaWplatBlackjack = await _db.BlackjackGames
                .SumAsync(g => (decimal?)(g.BetAmount + g.SplitBetAmount)) ?? 0,
            SumaWplatMines  = await _db.MinesGames.SumAsync(g => (decimal?)g.BetAmount) ?? 0,
            SumaWplatPlinko = await _db.PlinkoGames.SumAsync(g => (decimal?)g.BetAmount) ?? 0,
        };

        return View(vm);
    }
}

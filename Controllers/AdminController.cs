using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
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
    private readonly IKycService _kycService;

    public AdminController(Automaty db, IKycService kycService)
    {
        _db = db;
        _kycService = kycService;
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
            TotalBalance    = await _db.Wallets.SumAsync(w => w.BalanceReal + w.BalanceBonus),
            TotalDeposits   = await _db.StripePayments.SumAsync(p => (decimal?)p.Amount) ?? 0,
            TotalWithdrawals = await _db.StripeWithdrawals.SumAsync(w => (decimal?)w.Amount) ?? 0,
            RecentUsers     = await _db.Users
                                .Include(u => u.Wallet)
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

        var query = _db.Users.Include(u => u.Wallet).AsQueryable();

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

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return NotFound();

        wallet.BalanceReal = Math.Max(0, newBalance);
        wallet.BalanceBonus = 0;
        await _db.SaveChangesAsync();

        TempData["AdminMsg"] = $"Saldo uzytkownika {user.Email} zaktualizowane na {wallet.BalanceReal:F2} PLN.";
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

        model.Kod = model.Kod.ToUpper();
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

    // ─── KYC ────────────────────────────────────────────────────────────────

    public async Task<IActionResult> Kyc()
    {
        if (!IsAdmin()) return Forbid();

        var pending = await _kycService.GetPendingDocumentsAsync();
        var all = await _kycService.GetAllDocumentsAsync();

        var userIds = pending.Concat(all)
            .Select(d => d.UserId)
            .Distinct()
            .ToList();

        var userEmails = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.Email} ({u.Nazwa})");

        var vm = new AdminKycViewModel
        {
            PendingDocuments = pending,
            AllDocuments = all,
            UserEmails = userEmails
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveKyc(int documentId, string? comment)
    {
        if (!IsAdmin()) return Forbid();

        var adminId = GetAdminUserId();

        try
        {
            await _kycService.ApproveDocumentAsync(documentId, adminId, comment);
            TempData["AdminMsg"] = "Dokument zostal zatwierdzony.";
        }
        catch (Exception ex)
        {
            TempData["AdminMsg"] = $"Blad: {ex.Message}";
        }

        return RedirectToAction(nameof(Kyc));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectKyc(int documentId, string? comment)
    {
        if (!IsAdmin()) return Forbid();

        var adminId = GetAdminUserId();

        try
        {
            await _kycService.RejectDocumentAsync(documentId, adminId, comment);
            TempData["AdminMsg"] = "Dokument zostal odrzucony.";
        }
        catch (Exception ex)
        {
            TempData["AdminMsg"] = $"Blad: {ex.Message}";
        }

        return RedirectToAction(nameof(Kyc));
    }

    /// <summary>
    /// Podgląd pliku dokumentu (dla admina).
    /// </summary>
    public async Task<IActionResult> KycFile(int id)
    {
        if (!IsAdmin()) return Forbid();

        var document = await _kycService.GetDocumentByIdAsync(id);
        if (document == null)
            return NotFound();

        var path = _kycService.GetStoragePath(document);
        if (!System.IO.File.Exists(path))
            return NotFound();

        return PhysicalFile(path, document.ContentType, document.FileName);
    }

    private int GetAdminUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    // ─── STATYSTYKI GIER ─────────────────────────────────────────────────────

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

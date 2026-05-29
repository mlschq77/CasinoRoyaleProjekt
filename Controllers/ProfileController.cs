using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using CasinoRoyale.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly Automaty _dbContext;
    private readonly IKycService _kycService;
    private readonly IBalanceService _balanceService;

    public ProfileController(Automaty dbContext, IKycService kycService, IBalanceService balanceService)
    {
        _dbContext = dbContext;
        _kycService = kycService;
        _balanceService = balanceService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var profile = await BuildProfileViewModel(userId.Value);

        if (profile == null)
            return NotFound();

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = await _dbContext.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(user => user.Id == userId.Value);
        if (user == null)
            return NotFound();

        var newName = (model.Nazwa ?? string.Empty).Trim();
        var nameTaken = await _dbContext.Users
            .AnyAsync(existing => existing.Id != userId.Value && existing.Nazwa.ToLower() == newName.ToLower());

        if (nameTaken)
        {
            TempData["ProfileMessage"] = "Ta nazwa gracza jest juz zajeta.";
            return RedirectToAction(nameof(Index));
        }

        var newImie = (model.Imie ?? string.Empty).Trim();
        var newNazwisko = (model.Nazwisko ?? string.Empty).Trim();

        // Jeśli zmieniło się imię lub nazwisko — resetujemy KYC
        var daneOsoboweSieZmienily =
            !string.Equals(user.Imie, newImie, StringComparison.Ordinal) ||
            !string.Equals(user.Nazwisko, newNazwisko, StringComparison.Ordinal);

        user.Imie = newImie;
        user.Nazwisko = newNazwisko;
        user.Nazwa = newName;
        user.Email = (model.Email ?? string.Empty).Trim();

        await _dbContext.SaveChangesAsync();

        if (daneOsoboweSieZmienily && user.KycStatus != UserKycStatus.NotSubmitted)
        {
            await _kycService.ResetKycStatusAsync(user.Id);
            TempData["ProfileMessage"] = "Dane profilu zapisane. Zmiana imienia lub nazwiska spowodowala zresetowanie weryfikacji KYC — zaladuj ponownie dokumenty.";
        }
        else
        {
            TempData["ProfileMessage"] = "Dane profilu zapisane.";
        }

        await RefreshUserCookieAsync(user);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Bonuses()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var balanceInfo = await _balanceService.GetBalanceInfoAsync(userId.Value);

        // Pobierz szczegóły aktywnego bonusu (jeśli istnieje)
        var wallet = await _dbContext.Wallets
            .Where(w => w.UserId == userId.Value)
            .FirstOrDefaultAsync();

        List<ActiveBonusViewModel> activeBonuses = new();

        if (wallet?.ActiveBonusId != null)
        {
            var bonusData = await (
                from ub in _dbContext.UzyteKodyBonusowe
                join kb in _dbContext.KodyBonusowe on ub.KodBonusowyId equals kb.Id
                where ub.Id == wallet.ActiveBonusId
                select new
                {
                    ub.BonusAmount,
                    CodeName = kb.Kod
                }
            ).FirstOrDefaultAsync();

            if (bonusData != null)
            {
                activeBonuses.Add(new ActiveBonusViewModel
                {
                    CodeName = bonusData.CodeName,
                    BonusAmount = bonusData.BonusAmount,
                    RemainingAmount = wallet.BalanceBonus,
                    WageringProgress = wallet.WageringProgress ?? 0,
                    WageringRequired = wallet.WageringRequired ?? 0,
                    ExpiresAt = wallet.BonusExpiresAt
                });
            }
        }

        var vm = new ProfileBonusesViewModel
        {
            BalanceReal = balanceInfo?.BalanceReal ?? 0,
            BalanceBonus = balanceInfo?.BalanceBonus ?? 0,
            TotalWageringProgress = balanceInfo?.WageringProgress ?? 0,
            TotalWageringRequired = balanceInfo?.WageringRequired ?? 0,
            EarliestExpiry = balanceInfo?.ExpiresAt,
            Bonuses = activeBonuses
        };

        return View(vm);
    }

    private async Task<ProfileViewModel?> BuildProfileViewModel(int userId)
    {
        var userData = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.Wallet)
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.Id,
                user.Imie,
                user.Nazwisko,
                user.Nazwa,
                user.Email,
                BalanceReal = user.Wallet != null ? user.Wallet.BalanceReal : 0m,
                BalanceBonus = user.Wallet != null ? user.Wallet.BalanceBonus : 0m,
                user.DataRejestracji,
                user.KycStatus
            })
            .FirstOrDefaultAsync();

        if (userData == null) return null;

        var profile = new ProfileViewModel
        {
            Id = userData.Id,
            Imie = userData.Imie,
            Nazwisko = userData.Nazwisko,
            Nazwa = userData.Nazwa,
            Email = userData.Email,
            Balance = userData.BalanceReal,
            BalanceBonus = userData.BalanceBonus,
            DataRejestracji = userData.DataRejestracji,
            KycStatusDisplay = userData.KycStatus switch
            {
                CasinoRoyale.Models.UserKycStatus.NotSubmitted => "Nie przesłano",
                CasinoRoyale.Models.UserKycStatus.Pending => "Oczekuje na weryfikację",
                CasinoRoyale.Models.UserKycStatus.Approved => "Zweryfikowany",
                CasinoRoyale.Models.UserKycStatus.Rejected => "Odrzucony",
                _ => "Nieznany"
            },
            KycStatusCssClass = userData.KycStatus switch
            {
                CasinoRoyale.Models.UserKycStatus.NotSubmitted => "text-secondary",
                CasinoRoyale.Models.UserKycStatus.Pending => "text-warning",
                CasinoRoyale.Models.UserKycStatus.Approved => "text-success",
                CasinoRoyale.Models.UserKycStatus.Rejected => "text-danger",
                _ => "text-secondary"
            }
        };

        var payments = await _dbContext.StripePayments
            .AsNoTracking()
            .Where(payment => payment.UserId == userId)
            .Select(payment => new TransactionHistoryItemViewModel
            {
                Type = "Wplata",
                Amount = payment.Amount,
                Currency = payment.Currency.ToUpper(),
                CreatedAt = payment.CreatedAt
            })
            .ToListAsync();

        var withdrawals = await _dbContext.StripeWithdrawals
            .AsNoTracking()
            .Where(withdrawal => withdrawal.UserId == userId)
            .Select(withdrawal => new TransactionHistoryItemViewModel
            {
                Type = "Wyplata",
                Amount = withdrawal.Amount,
                Currency = withdrawal.Currency.ToUpper(),
                CreatedAt = withdrawal.CreatedAt
            })
            .ToListAsync();

        profile.TransactionHistory = payments
            .Concat(withdrawals)
            .OrderByDescending(item => item.CreatedAt)
            .Take(30)
            .ToList();

        var blackjackBets = await _dbContext.BlackjackGames
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .Select(game => new BetHistoryItemViewModel
            {
                GameName = "Blackjack",
                CreatedAt = game.CreatedAt,
                BetAmount = game.BetAmount + game.SplitBetAmount
            })
            .ToListAsync();

        var minesBets = await _dbContext.MinesGames
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .Select(game => new BetHistoryItemViewModel
            {
                GameName = "Mines",
                CreatedAt = game.CreatedAt,
                BetAmount = game.BetAmount
            })
            .ToListAsync();

        var plinkoBets = await _dbContext.PlinkoGames
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .Select(game => new BetHistoryItemViewModel
            {
                GameName = "Plinko",
                CreatedAt = game.CreatedAt,
                BetAmount = game.BetAmount
            })
            .ToListAsync();

        var crashBets = await _dbContext.CrashSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .Select(session => new BetHistoryItemViewModel
            {
                GameName = "Crash",
                CreatedAt = session.CreatedAt,
                BetAmount = session.BetAmount
            })
            .ToListAsync();

        var diceBets = await _dbContext.DiceGames
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .Select(game => new BetHistoryItemViewModel
            {
                GameName = "Dice",
                CreatedAt = game.CreatedAt,
                BetAmount = game.BetAmount
            })
            .ToListAsync();

        var kenoBets = await _dbContext.KenoGames
            .AsNoTracking()
            .Where(game => game.UserId == userId)
            .Select(game => new BetHistoryItemViewModel
            {
                GameName = "Keno",
                CreatedAt = game.CreatedAt,
                BetAmount = game.BetAmount
            })
            .ToListAsync();

        profile.BetHistory = blackjackBets
            .Concat(minesBets)
            .Concat(plinkoBets)
            .Concat(crashBets)
            .Concat(diceBets)
            .Concat(kenoBets)
            .OrderByDescending(item => item.CreatedAt)
            .Take(40)
            .ToList();

        return profile;
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    private async Task RefreshUserCookieAsync(Models.User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Nazwa),
            new(ClaimTypes.GivenName, user.Imie),
            new(ClaimTypes.Surname, user.Nazwisko),
            new(ClaimTypes.Email, user.Email),
            new("IsAdmin", user.IsAdmin.ToString()),
            new("Balance", (user.Wallet?.BalanceReal ?? 0m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}

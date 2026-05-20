using CasinoRoyale.Data;
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

    public ProfileController(Automaty dbContext)
    {
        _dbContext = dbContext;
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

        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId.Value);
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

        user.Imie = (model.Imie ?? string.Empty).Trim();
        user.Nazwisko = (model.Nazwisko ?? string.Empty).Trim();
        user.Nazwa = newName;
        user.Email = (model.Email ?? string.Empty).Trim();

        await _dbContext.SaveChangesAsync();
        await RefreshUserCookieAsync(user);

        TempData["ProfileMessage"] = "Dane profilu zapisane.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<ProfileViewModel?> BuildProfileViewModel(int userId)
    {
        var profile = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new ProfileViewModel
            {
                Id = user.Id,
                Imie = user.Imie,
                Nazwisko = user.Nazwisko,
                Nazwa = user.Nazwa,
                Email = user.Email,
                Balance = user.Balance,
                DataRejestracji = user.DataRejestracji
            })
            .FirstOrDefaultAsync();

        if (profile == null) return null;

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

        profile.BetHistory = blackjackBets
            .Concat(minesBets)
            .Concat(plinkoBets)
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
            new("Balance", user.Balance.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}

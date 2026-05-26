using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

public class AuthController : Controller
{
	private readonly Automaty _db;

	public AuthController(Automaty db)
	{
		_db = db;
	}

	// ── REJESTRACJA ──────────────────────────────────────

	[HttpGet]
	public IActionResult Rejestracja()
	{
		if (User.Identity?.IsAuthenticated == true)
			return RedirectToAction("Index", "Home");

		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Rejestracja(RejestracjaViewModel model)
	{
		if (!ModelState.IsValid)
			return View(model);

		bool emailZajety = await _db.Users
			.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());

		if (emailZajety)
		{
			ModelState.AddModelError("Email", "Ten adres e-mail jest już zajęty.");
			return View(model);
		}

		bool nazwaZajeta = await _db.Users
			.AnyAsync(u => u.Nazwa.ToLower() == model.Nazwa.ToLower());

		if (nazwaZajeta)
		{
			ModelState.AddModelError("Nazwa", "Ta nazwa użytkownika jest już zajęta.");
			return View(model);
		}

		var user = new User
		{
			Imie = model.Imie,
			Nazwisko = model.Nazwisko,
			Nazwa = model.Nazwa,
			Email = model.Email,
			HasloHash = BCrypt.Net.BCrypt.HashPassword(model.Haslo),
			BalanceReal = 1000m,
			DataRejestracji = DateTime.UtcNow
		};

		_db.Users.Add(user);
		await _db.SaveChangesAsync();

		await ZalogujUzytkownika(user, false);

		return RedirectToAction("Index", "Home");
	}

	// ── LOGOWANIE ────────────────────────────────────────

	[HttpGet]
	public IActionResult Logowanie(string? returnUrl = null)
	{
		if (User.Identity?.IsAuthenticated == true)
			return RedirectToAction("Index", "Home");

		ViewBag.ReturnUrl = returnUrl;
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Logowanie(LogowanieViewModel model, string? returnUrl = null)
	{
		ViewBag.ReturnUrl = returnUrl;

		if (!ModelState.IsValid)
			return View(model);

		var user = await _db.Users
			.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

		if (user == null || !BCrypt.Net.BCrypt.Verify(model.Haslo, user.HasloHash))
		{
			ModelState.AddModelError(string.Empty, "Nieprawidłowy e-mail lub hasło.");
			return View(model);
		}

		await ZalogujUzytkownika(user, model.ZapamiętajMnie);

		if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
			return Redirect(returnUrl);

		return RedirectToAction("Index", "Home");
	}

	// ── WYLOGOWANIE ──────────────────────────────────────

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Wylogowanie()
	{
		await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
		return RedirectToAction("Index", "Home");
	}

	// ── POMOCNICZE ───────────────────────────────────────

	private async Task ZalogujUzytkownika(User user, bool pamietajMnie)
	{
		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.Nazwa),
			new Claim(ClaimTypes.GivenName, user.Imie),
			new Claim(ClaimTypes.Surname, user.Nazwisko),
			new Claim(ClaimTypes.Email, user.Email), 
			new Claim("Balance", user.BalanceReal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture))
		};

		var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
		var principal = new ClaimsPrincipal(identity);

		var authProperties = new AuthenticationProperties
		{
			IsPersistent = pamietajMnie,
			ExpiresUtc = pamietajMnie
				? DateTimeOffset.UtcNow.AddDays(30)
				: DateTimeOffset.UtcNow.AddHours(2)
		};

		await HttpContext.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme,
			principal,
			authProperties);
	}
}

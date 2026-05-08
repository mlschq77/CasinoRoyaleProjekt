using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using CasinoRoyale.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace CasinoRoyale.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private const string Currency = "pln";
    private const string FallbackTestSecretKey = "sk_test_51TUcQdPPVxf1VniAZ8rqcE1bysvsqwhfayIUIiLSFYtx71ynbRZH7hNZhSjoILfYDTWRt3cFypO38LS6I3Ksgb3J002cMQc8s5";
    private readonly Automaty _dbContext;
    private readonly IBalanceService _balanceService;
    private readonly IConfiguration _configuration;

    public PaymentsController(Automaty dbContext, IBalanceService balanceService, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _balanceService = balanceService;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Deposit(string? message = null)
    {
        return View(new DepositViewModel
        {
            Message = message,
            StripeConfigured = IsStripeConfigured()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCheckoutSession(decimal amount)
    {
        if (!IsStripeConfigured())
            return RedirectToAction(nameof(Deposit), new { message = "Stripe test nie jest jeszcze skonfigurowany." });

        if (amount < 10 || amount > 10000)
            return RedirectToAction(nameof(Deposit), new { message = "Kwota doladowania musi byc w zakresie 10-10000." });

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        StripeConfiguration.ApiKey = GetStripeSecretKey();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{baseUrl}{Url.Action(nameof(Success), "Payments")}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}{Url.Action(nameof(Cancel), "Payments")}",
            CustomerEmail = User.FindFirstValue(ClaimTypes.Email),
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.Value.ToString(),
                ["amount"] = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currency,
                        UnitAmount = (long)(amount * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Doladowanie balansu CasinoRoyale"
                        }
                    }
                }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Redirect(session.Url);
    }

    [HttpGet]
    public async Task<IActionResult> Success(string session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id))
            return RedirectToAction(nameof(Deposit), new { message = "Brak identyfikatora sesji Stripe." });

        if (!IsStripeConfigured())
            return RedirectToAction(nameof(Deposit), new { message = "Stripe test nie jest jeszcze skonfigurowany." });

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        StripeConfiguration.ApiKey = GetStripeSecretKey();

        var service = new SessionService();
        var session = await service.GetAsync(session_id);

        if (session.PaymentStatus != "paid")
            return RedirectToAction(nameof(Deposit), new { message = "Platnosc nie zostala potwierdzona." });

        if (!session.Metadata.TryGetValue("userId", out var metadataUserId) ||
            metadataUserId != userId.Value.ToString())
        {
            return RedirectToAction(nameof(Deposit), new { message = "Ta platnosc nie nalezy do aktualnego uzytkownika." });
        }

        var amount = (session.AmountTotal ?? 0) / 100m;
        if (amount <= 0)
            return RedirectToAction(nameof(Deposit), new { message = "Nieprawidlowa kwota platnosci." });

        var alreadyProcessed = await _dbContext.StripePayments.AnyAsync(payment => payment.SessionId == session.Id);
        if (alreadyProcessed)
            return RedirectToAction(nameof(Deposit), new { message = "Ta platnosc byla juz zaksiegowana." });

        _dbContext.StripePayments.Add(new StripePayment
        {
            UserId = userId.Value,
            SessionId = session.Id,
            Amount = amount,
            Currency = session.Currency ?? Currency,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        var payoutResult = await _balanceService.PayoutAsync(userId.Value, amount);
        if (!payoutResult.Success)
            return RedirectToAction(nameof(Deposit), new { message = payoutResult.Error });

        return RedirectToAction(nameof(Deposit), new { message = $"Doladowano balans o {amount:0.00} PLN." });
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        return RedirectToAction(nameof(Deposit), new { message = "Platnosc zostala anulowana." });
    }

    private string? GetStripeSecretKey()
    {
        var configuredKey = _configuration["Stripe:SecretKey"]?.Trim();
        return string.IsNullOrWhiteSpace(configuredKey)
            ? FallbackTestSecretKey
            : configuredKey;
    }

    private bool IsStripeConfigured()
    {
        var secretKey = GetStripeSecretKey();
        return !string.IsNullOrWhiteSpace(secretKey) && secretKey.StartsWith("sk_test_", StringComparison.Ordinal);
    }

    private int? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }
}

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
    private readonly IBonusCodeService _bonusCodeService;
    private readonly IConfiguration _configuration;

    public PaymentsController(
        Automaty dbContext,
        IBalanceService balanceService,
        IBonusCodeService bonusCodeService,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _balanceService = balanceService;
        _bonusCodeService = bonusCodeService;
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

    [HttpGet]
    public async Task<IActionResult> ValidateBonusCode(string? kodBonusowy, decimal amount)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var validation = await _bonusCodeService.ValidateAsync(userId.Value, kodBonusowy, amount);
        if (validation.Error != null)
            return Json(new { isValid = false, message = validation.Error });

        if (validation.KodBonusowy == null)
            return Json(new { isValid = true, message = string.Empty });

        return Json(new
        {
            isValid = true,
            message = $"Kod poprawny. Szacowany bonus: {validation.BonusAmount:0.00} PLN."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCheckoutSession(DepositViewModel model)
    {
        model.StripeConfigured = IsStripeConfigured();

        if (!ModelState.IsValid)
            return View(nameof(Deposit), model);

        if (!IsStripeConfigured())
            return RedirectToAction(nameof(Deposit), new { message = "Stripe test nie jest jeszcze skonfigurowany." });

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var normalizedBonusCode = model.KodBonusowy?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedBonusCode))
        {
            var validation = await _bonusCodeService.ValidateAsync(userId.Value, normalizedBonusCode, model.Amount);
            if (validation.Error != null)
            {
                ModelState.AddModelError(nameof(DepositViewModel.KodBonusowy), validation.Error);
                return View(nameof(Deposit), model);
            }

            model.KodBonusowy = normalizedBonusCode;
        }

        StripeConfiguration.ApiKey = GetStripeSecretKey();

        var metadata = new Dictionary<string, string>
        {
            ["userId"] = userId.Value.ToString(),
            ["amount"] = model.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(normalizedBonusCode))
            metadata["kodBonusowy"] = normalizedBonusCode;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{baseUrl}{Url.Action(nameof(Success), "Payments")}?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}{Url.Action(nameof(Cancel), "Payments")}",
            CustomerEmail = User.FindFirstValue(ClaimTypes.Email),
            Metadata = metadata,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currency,
                        UnitAmount = (long)(model.Amount * 100),
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

        if (!session.Metadata.TryGetValue("userId", out var metadataUserId) || metadataUserId != userId.Value.ToString())
        {
            return RedirectToAction(nameof(Deposit), new { message = "Ta platnosc nie nalezy do aktualnego uzytkownika." });
        }

        var paidAmount = (session.AmountTotal ?? 0) / 100m;
        if (paidAmount <= 0)
            return RedirectToAction(nameof(Deposit), new { message = "Nieprawidlowa kwota platnosci." });

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var alreadyProcessed = await _dbContext.StripePayments.AnyAsync(payment => payment.SessionId == session.Id);
            if (alreadyProcessed)
            {
                await transaction.RollbackAsync();
                return RedirectToAction(nameof(Deposit), new { message = "Ta platnosc byla juz zaksiegowana." });
            }

        var stripePayment = new StripePayment
            {
                UserId = userId.Value,
                SessionId = session.Id,
                Amount = paidAmount,
                Currency = session.Currency ?? Currency,
                CreatedAt = DateTime.UtcNow
        };
            _dbContext.StripePayments.Add(stripePayment);

            await _dbContext.SaveChangesAsync();

            session.Metadata.TryGetValue("kodBonusowy", out var kodString);
            var bonusResult = await _bonusCodeService.ApplyAsync(userId.Value, kodString, paidAmount, stripePayment);
            var bonusAmount = bonusResult.BonusAmount;

            // Wp�ata idzie na BalanceReal, bonus na BalanceBonus (osobno)
            var payoutResult = await _balanceService.PayoutAsync(userId.Value, paidAmount);

            if (!payoutResult.Success)
            {
                await transaction.RollbackAsync();
                return RedirectToAction(nameof(Deposit), new { message = payoutResult.Error });
            }

            // Dodaj bonus do BalanceBonus przez UzytyKodBonusowy (ju� zapisany w ApplyAsync)
            if (bonusAmount > 0)
            {
                // BalanceBonus jest aktualizowany przez RefreshBalanceBonusAsync w BalanceService
                // po dodaniu wpisu UzytyKodBonusowy przez ApplyAsync
            }

            await transaction.CommitAsync();

            var wageringInfo = bonusAmount > 0 && bonusResult.WageringRequired > 0
                ? $" Musisz wykonac obrot w wysokosci {bonusResult.WageringRequired:0.00} PLN przed wyp�at�."
                : string.Empty;

            var successMessage = bonusAmount > 0
                ? $"Zasilenie {paidAmount:0.00} PLN udane! Otrzymujesz {bonusAmount:0.00} PLN bonusu. Bonus wygasa {bonusResult.ExpiresAt:. dd.MM.yyyy}.{wageringInfo}"
                : bonusResult.AlreadyUsed
                    ? $"Doladowano balans o {paidAmount:0.00} PLN. Kod bonusowy byl juz wykorzystany, wiec bonus nie zostal naliczony."
                    : $"Doladowano balans o {paidAmount:0.00} PLN.";

            return RedirectToAction(nameof(Deposit), new { message = successMessage });
        });
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        return RedirectToAction(nameof(Deposit), new { message = "Platnosc zostala anulowana." });
    }

    [HttpGet]
    public IActionResult Withdraw(string? message = null)
    {
        return View(new WithdrawViewModel
        {
            Message = message,
            StripeConfigured = IsStripeConfigured()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTransfer(decimal amount, string destinationAccountId)
    {
        if (!IsStripeConfigured())
            return RedirectToAction(nameof(Withdraw), new { message = "Stripe test nie jest jeszcze skonfigurowany." });

        if (amount < 10 || amount > 10000)
            return RedirectToAction(nameof(Withdraw), new { message = "Kwota wyplaty musi byc w zakresie 10-10000." });

        destinationAccountId = destinationAccountId?.Trim() ?? string.Empty;
        if (!destinationAccountId.StartsWith("acct_", StringComparison.Ordinal))
            return RedirectToAction(nameof(Withdraw), new { message = "Podaj poprawne konto Stripe Connect zaczynajace sie od acct_." });

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var withdrawResult = await _balanceService.WithdrawAsync(userId.Value, amount);
        if (!withdrawResult.Success)
            return RedirectToAction(nameof(Withdraw), new { message = withdrawResult.Error });

        StripeConfiguration.ApiKey = GetStripeSecretKey();

        try
        {
            var service = new TransferService();
            var transfer = await service.CreateAsync(new TransferCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = Currency,
                Destination = destinationAccountId,
                Description = $"Wyplata CasinoRoyale dla uzytkownika {userId.Value}",
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.Value.ToString(),
                    ["amount"] = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                }
            });

            _dbContext.StripeWithdrawals.Add(new StripeWithdrawal
            {
                UserId = userId.Value,
                TransferId = transfer.Id,
                DestinationAccountId = destinationAccountId,
                Amount = amount,
                Currency = transfer.Currency ?? Currency,
                CreatedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Withdraw), new { message = $"Zlecono wyplate {amount:0.00} PLN przez Stripe." });
        }
        catch (StripeException ex)
        {
            await _balanceService.PayoutAsync(userId.Value, amount);
            return RedirectToAction(nameof(Withdraw), new { message = $"Stripe odrzucil wyplate: {ex.StripeError?.Message ?? ex.Message}" });
        }
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

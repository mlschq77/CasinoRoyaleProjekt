using CasinoRoyale.Models;

namespace CasinoRoyale.Services;

public interface IBonusCodeService
{
    Task<BonusCodeValidationResult> ValidateAsync(int userId, string? kodBonusowy, decimal amount);
    Task<BonusCodeApplicationResult> ApplyAsync(int userId, string? kodBonusowy, decimal paidAmount, StripePayment stripePayment);
}

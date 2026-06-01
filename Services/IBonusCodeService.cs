using CasinoRoyale.Models;

namespace CasinoRoyale.Services;

public interface IBonusCodeService
{
    Task<BonusCodeValidationResult> ValidateAsync(int userId, string? kodBonusowy, decimal amount);
    Task<BonusCodeApplicationResult> ApplyAsync(int userId, string? kodBonusowy, decimal paidAmount, StripePayment stripePayment);

    /// <summary>Aktualizuje postep wageringu w Wallet i wykonuje auto-konwersje bonusu na real jesli spelniomy.</summary>
    Task TrackWageringProgressAsync(Wallet wallet, decimal amountFromBonus);

    /// <summary>Czyści pola aktywnego bonusu w Wallet (ActiveBonusId, WageringRequired, WageringProgress, BonusExpiresAt).</summary>
    void ClearActiveBonus(Wallet wallet);

    /// <summary>Konwertuje pozostaly BalanceBonus na BalanceReal (gdy wagering spelniony).</summary>
    void ConvertBonusToReal(Wallet wallet);

    /// <summary>Sprawdza czy aktywny bonus wygasl i jesli tak — anuluje go.</summary>
    Task ExpireActiveBonusIfNeededAsync(Wallet wallet);

    /// <summary>Anuluje aktywny bonus (jesli wagering spelniony — konwertuje na real, jesli nie — usuwa).</summary>
    Task CancelActiveBonusAsync(Wallet wallet);
}

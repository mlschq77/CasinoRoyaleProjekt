using System.Text.RegularExpressions;
using CasinoRoyale.Data;
using CasinoRoyale.Models;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Services;

public class BonusCodeService : IBonusCodeService
{
    private readonly Automaty _dbContext;

    public BonusCodeService(Automaty dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BonusCodeValidationResult> ValidateAsync(int userId, string? kodBonusowy, decimal amount)
    {
        var normalizedBonusCode = kodBonusowy?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedBonusCode))
            return new BonusCodeValidationResult();

        if (normalizedBonusCode.Length > 64)
            return Failed("Kod bonusowy moze miec maksymalnie 64 znaki.");

        if (!Regex.IsMatch(normalizedBonusCode, "^[A-Za-z0-9_-]+$"))
            return Failed("Kod bonusowy moze zawierac tylko litery, cyfry, myslnik i podkreslenie.");

        var bonusCode = await _dbContext.KodyBonusowe
            .AsNoTracking()
            .FirstOrDefaultAsync(kod => kod.Kod.ToLower() == normalizedBonusCode.ToLower());

        if (bonusCode == null)
            return Failed("Podany kod bonusowy nie istnieje.");

        var codeAlreadyUsed = await _dbContext.UzyteKodyBonusowe
            .AnyAsync(uzytyKod => uzytyKod.UserId == userId && uzytyKod.KodBonusowyId == bonusCode.Id);

        if (codeAlreadyUsed)
            return Failed("Ten kod bonusowy zostal juz przez Ciebie wykorzystany.", alreadyUsed: true);

        if (bonusCode.WaznyDo.HasValue && bonusCode.WaznyDo.Value < DateTime.UtcNow)
            return Failed("Podany kod bonusowy stracil waznosc.");

        if (amount < bonusCode.MinimalnaWplata)
            return Failed($"Ten kod wymaga minimalnej wplaty {bonusCode.MinimalnaWplata:0.00} PLN.");

        return new BonusCodeValidationResult
        {
            KodBonusowy = bonusCode,
            BonusAmount = CalculateBonus(amount, bonusCode)
        };
    }

    public async Task<BonusCodeApplicationResult> ApplyAsync(int userId, string? kodBonusowy, decimal paidAmount, StripePayment stripePayment)
    {
        var validation = await ValidateAsync(userId, kodBonusowy, paidAmount);
        if (!validation.IsValid || validation.KodBonusowy == null)
        {
            return new BonusCodeApplicationResult
            {
                AlreadyUsed = validation.AlreadyUsed
            };
        }

        var bonusCode = validation.KodBonusowy;
        var bonusAmount = validation.BonusAmount;

        var wageringRequired =  bonusAmount * bonusCode.WageringMultiplier;

        var expiresAt = DateTime.UtcNow.AddDays(7);

        var bonusEntry = new UzytyKodBonusowy
        {
            UserId = userId,
            KodBonusowyId = bonusCode.Id,
            StripePaymentId = stripePayment.Id,
            SessionId = stripePayment.SessionId,
            Uzyto = DateTime.UtcNow,
            BonusAmount = bonusAmount
        };

        _dbContext.UzyteKodyBonusowe.Add(bonusEntry);

        try
        {
            await _dbContext.SaveChangesAsync();

            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet != null)
            {
                wallet.ActiveBonusId = bonusEntry.Id;
                wallet.BalanceBonus = bonusAmount;
                wallet.WageringRequired = wageringRequired;
                wallet.WageringProgress = 0;
                wallet.BonusExpiresAt = expiresAt;
            }

            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            DetachPendingBonusCodeUsages();
            return new BonusCodeApplicationResult { AlreadyUsed = true };
        }

        return new BonusCodeApplicationResult
        {
            BonusAmount = bonusAmount,
            WageringRequired = wageringRequired,
            ExpiresAt = expiresAt
        };
    }

    public async Task TrackWageringProgressAsync(Wallet wallet, decimal amountFromBonus)
    {
        if (wallet.WageringProgress == null || wallet.WageringRequired == null)
            return;

        wallet.WageringProgress += amountFromBonus;

        if (wallet.WageringProgress < wallet.WageringRequired)
            return;

        ClearActiveBonus(wallet);
        ConvertBonusToReal(wallet);
    }

    public void ClearActiveBonus(Wallet wallet)
    {
        wallet.ActiveBonusId = null;
        wallet.WageringRequired = null;
        wallet.WageringProgress = null;
        wallet.BonusExpiresAt = null;
    }

    public void ConvertBonusToReal(Wallet wallet)
    {
        if (wallet.BalanceBonus <= 0) return;
        wallet.BalanceReal += wallet.BalanceBonus;
        wallet.BalanceBonus = 0;
    }

    public async Task ExpireActiveBonusIfNeededAsync(Wallet wallet)
    {
        if (wallet.ActiveBonusId == null || wallet.BonusExpiresAt == null)
            return;

        if (wallet.BonusExpiresAt >= DateTime.UtcNow)
            return;

        wallet.BalanceBonus = 0;
        ClearActiveBonus(wallet);
    }

    public async Task CancelActiveBonusAsync(Wallet wallet)
    {
        if (wallet.ActiveBonusId == null) return;

        if (wallet.WageringProgress != null && wallet.WageringRequired != null
            && wallet.WageringProgress >= wallet.WageringRequired)
        {
            ConvertBonusToReal(wallet);
        }
        else
        {
            wallet.BalanceBonus = 0;
        }

        ClearActiveBonus(wallet);
    }

    private static BonusCodeValidationResult Failed(string error, bool alreadyUsed = false)
    {
        return new BonusCodeValidationResult
        {
            Error = error,
            AlreadyUsed = alreadyUsed
        };
    }

    private static decimal CalculateBonus(decimal amount, KodBonusowy bonusCode)
    {
        return (amount * (bonusCode.BonusProcentowy / 100m)) + bonusCode.BonusKwotowy;
    }

    private void DetachPendingBonusCodeUsages()
    {
        var pendingEntries = _dbContext.ChangeTracker
            .Entries<UzytyKodBonusowy>()
            .Where(entry => entry.State == EntityState.Added)
            .ToList();

        foreach (var entry in pendingEntries)
        {
            entry.State = EntityState.Detached;
        }
    }
}

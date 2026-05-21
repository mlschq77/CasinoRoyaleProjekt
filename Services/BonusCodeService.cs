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
        var normalizedBonusCode = kodBonusowy?.Trim();
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

        _dbContext.UzyteKodyBonusowe.Add(new UzytyKodBonusowy
        {
            UserId = userId,
            KodBonusowyId = validation.KodBonusowy.Id,
            StripePaymentId = stripePayment.Id,
            SessionId = stripePayment.SessionId,
            Uzyto = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            DetachPendingBonusCodeUsages();
            return new BonusCodeApplicationResult { AlreadyUsed = true };
        }

        return new BonusCodeApplicationResult
        {
            BonusAmount = validation.BonusAmount
        };
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

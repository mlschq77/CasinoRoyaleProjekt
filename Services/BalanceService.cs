using CasinoRoyale.Data;
using CasinoRoyale.Models;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Services;

public class BalanceService : IBalanceService
{
    private readonly Automaty _dbContext;
    private readonly IBonusCodeService _bonusCodeService;

    public BalanceService(Automaty dbContext, IBonusCodeService bonusCodeService)
    {
        _dbContext = dbContext;
        _bonusCodeService = bonusCodeService;
    }

    public async Task<decimal?> GetBalanceAsync(int userId)
    {
        return await _dbContext.Wallets
            .Where(w => w.UserId == userId)
            .Select(w => (decimal?)(w.BalanceReal + w.BalanceBonus))
            .FirstOrDefaultAsync();
    }

    public async Task<BalanceInfo?> GetBalanceInfoAsync(int userId)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null) return null;

        await _bonusCodeService.ExpireActiveBonusIfNeededAsync(wallet);

        return new BalanceInfo
        {
            BalanceReal = wallet.BalanceReal,
            BalanceBonus = wallet.BalanceBonus,
            WageringRequired = wallet.WageringRequired ?? 0,
            WageringProgress = wallet.WageringProgress ?? 0,
            ExpiresAt = wallet.BonusExpiresAt
        };
    }

    public async Task<BalanceResult> PlaceBetAsync(int userId, decimal amount, string? sessionKey = null, string? gameName = null)
    {
        if (amount <= 0)
            return BalanceResult.Failed("Stawka musi byc wieksza od zera.");

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return BalanceResult.Failed("Nie znaleziono portfela uzytkownika.");

        await _bonusCodeService.ExpireActiveBonusIfNeededAsync(wallet);

        var totalBalance = wallet.BalanceReal + wallet.BalanceBonus;
        if (totalBalance < amount)
            return BalanceResult.Failed("Brak wystarczajacych srodkow.", wallet.BalanceReal, wallet.BalanceBonus);

        sessionKey ??= Guid.NewGuid().ToString("N");

        decimal amountFromBonus = 0;
        decimal amountFromReal = amount;

        if (wallet.BalanceBonus > 0)
        {
            amountFromBonus = Math.Min(wallet.BalanceBonus, amount);
            amountFromReal = amount - amountFromBonus;

            wallet.BalanceBonus -= amountFromBonus;
            if (wallet.BalanceBonus < 0) wallet.BalanceBonus = 0;

            await _bonusCodeService.TrackWageringProgressAsync(wallet, amountFromBonus);
        }

        if (amountFromReal > 0)
        {
            wallet.BalanceReal -= amountFromReal;
        }

        _dbContext.BetRecords.Add(new BetRecord
        {
            UserId = userId,
            Amount = amount,
            AmountFromBonus = amountFromBonus,
            SessionKey = sessionKey,
            GameName = gameName ?? string.Empty,
            BonusDeductions = null,
            Settled = false,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        return BalanceResult.Ok(wallet.BalanceReal, wallet.BalanceBonus, amountFromBonus);
    }

    public async Task<BalanceResult> PayoutAsync(int userId, decimal amount, string? sessionKey = null)
    {
        if (amount < 0)
            return BalanceResult.Failed("Wyplata nie moze byc ujemna.");

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return BalanceResult.Failed("Nie znaleziono portfela uzytkownika.");

        decimal amountToReal = amount;
        decimal amountToBonus = 0;

        if (sessionKey != null)
        {
            var unsettledRecords = await _dbContext.BetRecords
                .Where(r => r.UserId == userId && r.SessionKey == sessionKey && !r.Settled)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            if (unsettledRecords.Count > 0)
            {
                var totalAmount = unsettledRecords.Sum(r => r.Amount);
                var totalAmountFromBonus = unsettledRecords.Sum(r => r.AmountFromBonus);

                var bonusRatio = totalAmount > 0 ? totalAmountFromBonus / totalAmount : 0;
                amountToBonus = amount * bonusRatio;
                amountToReal = amount - amountToBonus;

                foreach (var record in unsettledRecords)
                {
                    record.Settled = true;
                }
            }
        }

        if (amountToBonus > 0)
        {
            wallet.BalanceBonus += amountToBonus;
        }

        if (amountToReal > 0)
        {
            wallet.BalanceReal += amountToReal;
        }

        if (wallet.WageringProgress == null || wallet.WageringRequired == null)
            _bonusCodeService.ConvertBonusToReal(wallet);

            await _dbContext.SaveChangesAsync();

        var updatedWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        return BalanceResult.Ok(updatedWallet!.BalanceReal, updatedWallet.BalanceBonus);
    }

    public async Task<BalanceResult> WithdrawAsync(int userId, decimal amount)
    {
        if (amount <= 0)
            return BalanceResult.Failed("Kwota wyplaty musi byc wieksza od zera.");

        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return BalanceResult.Failed("Nie znaleziono portfela uzytkownika.");

        await _bonusCodeService.ExpireActiveBonusIfNeededAsync(wallet);

        if (wallet.BalanceBonus > 0)
        {
            await _bonusCodeService.CancelActiveBonusAsync(wallet);
        }

        if (wallet.BalanceReal < amount)
            return BalanceResult.Failed("Brak wystarczajacych srodkow.", wallet.BalanceReal, wallet.BalanceBonus);

        wallet.BalanceReal -= amount;
        await _dbContext.SaveChangesAsync();

        return BalanceResult.Ok(wallet.BalanceReal, wallet.BalanceBonus);
    }

    public async Task<BalanceResult> CheckAndCancelBonusAsync(int userId)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return BalanceResult.Failed("Nie znaleziono portfela uzytkownika.");

        await _bonusCodeService.ExpireActiveBonusIfNeededAsync(wallet);

        if (wallet.ActiveBonusId != null)
        {
            await _bonusCodeService.CancelActiveBonusAsync(wallet);
            await _dbContext.SaveChangesAsync();
        }

        return BalanceResult.Ok(wallet.BalanceReal, wallet.BalanceBonus);
    }

}

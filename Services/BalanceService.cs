using CasinoRoyale.Data;
using CasinoRoyale.Models;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale.Services;

public class BalanceService : IBalanceService
{
    private readonly Automaty _dbContext;

    public BalanceService(Automaty dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal?> GetBalanceAsync(int userId)
    {
        return await _dbContext.Users
            .Where(user => user.Id == userId)
            .Select(user => (decimal?)(user.BalanceReal + user.BalanceBonus))
            .FirstOrDefaultAsync();
    }

    public async Task<BalanceInfo?> GetBalanceInfoAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return null;

        await ExpireActiveBonusesAsync(userId);
        await RefreshBalanceBonusAsync(userId);
        user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return null;

        var activeBonuses = await _dbContext.UzyteKodyBonusowe
            .Where(b => b.UserId == userId && b.Status == BonusStatus.Active)
            .ToListAsync();

        return new BalanceInfo
        {
            BalanceReal = user.BalanceReal,
            BalanceBonus = user.BalanceBonus,
            WageringRequired = activeBonuses.Sum(b => b.WageringRequired),
            WageringProgress = activeBonuses.Sum(b => b.WageringProgress),
            ExpiresAt = activeBonuses.Any()
                ? activeBonuses.Min(b => b.ExpiresAt)
                : null
        };
    }

    public async Task<BalanceResult> PlaceBetAsync(int userId, decimal amount, string? sessionKey = null)
    {
        if (amount <= 0)
            return BalanceResult.Failed("Stawka musi byc wieksza od zera.");

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return BalanceResult.Failed("Nie znaleziono uzytkownika.");

        await ExpireActiveBonusesAsync(userId);
        await RefreshBalanceBonusAsync(userId);
        user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return BalanceResult.Failed("Nie znaleziono uzytkownika.");

        var totalBalance = user.BalanceReal + user.BalanceBonus;
        if (totalBalance < amount)
            return BalanceResult.Failed("Brak wystarczajacych srodkow.", user.BalanceReal, user.BalanceBonus);

        sessionKey ??= Guid.NewGuid().ToString("N");

        decimal amountFromBonus = 0;
        decimal amountFromReal = amount;
        string? bonusDeductionsStr = null;

        if (user.BalanceBonus > 0)
        {
            amountFromBonus = Math.Min(user.BalanceBonus, amount);
            amountFromReal = amount - amountFromBonus;

            var activeBonuses = await _dbContext.UzyteKodyBonusowe
                .Where(b => b.UserId == userId && b.Status == BonusStatus.Active)
                .OrderBy(b => b.Uzyto)
                .ToListAsync();

            var remainingToDeduct = amountFromBonus;
            var perBonusDeductions = new Dictionary<int, decimal>();
            foreach (var bonus in activeBonuses)
            {
                if (remainingToDeduct <= 0) break;
                var deduct = Math.Min(bonus.RemainingAmount, remainingToDeduct);
                bonus.RemainingAmount -= deduct;
                perBonusDeductions[bonus.Id] = deduct;
                remainingToDeduct -= deduct;
            }

            // Wagering A: tylko faktycznie odjęta kwota z danego bonusu
            foreach (var bonus in activeBonuses)
            {
                if (perBonusDeductions.TryGetValue(bonus.Id, out var deducted))
                {
                    bonus.WageringProgress += deducted;
                }
            }

            // Auto-konwersja: jeśli wagering właśnie został spełniony, przenieś na real
            foreach (var bonus in activeBonuses)
            {
                if (perBonusDeductions.TryGetValue(bonus.Id, out var deducted) && deducted > 0
                    && bonus.WageringProgress >= bonus.WageringRequired)
                {
                    var convertAmount = bonus.RemainingAmount;
                    if (convertAmount > 0)
                    {
                        user.BalanceReal += convertAmount;
                    }
                    bonus.RemainingAmount = 0;
                    bonus.Status = BonusStatus.WageringMet;
                }
            }

            // Zapisz per-bonus deductions do BetRecord
            bonusDeductionsStr = string.Join(";", perBonusDeductions
                .Where(d => d.Value > 0)
                .Select(d => $"{d.Key}:{d.Value}"));

            user.BalanceBonus -= amountFromBonus;
            if (user.BalanceBonus < 0) user.BalanceBonus = 0;

            await RefreshBalanceBonusAsync(userId);
        }

        if (amountFromReal > 0)
        {
            user.BalanceReal -= amountFromReal;
        }

        _dbContext.BetRecords.Add(new BetRecord
        {
            UserId = userId,
            Amount = amount,
            AmountFromBonus = amountFromBonus,
            SessionKey = sessionKey,
            BonusDeductions = amountFromBonus > 0 ? bonusDeductionsStr : null,
            Settled = false,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        return BalanceResult.Ok(user.BalanceReal, user.BalanceBonus, amountFromBonus);
    }

    public async Task<BalanceResult> PayoutAsync(int userId, decimal amount, string? sessionKey = null)
    {
        if (amount < 0)
            return BalanceResult.Failed("Wyplata nie moze byc ujemna.");

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return BalanceResult.Failed("Nie znaleziono uzytkownika.");

        decimal amountToReal = amount;
        decimal amountToBonus = 0;

        List<BetRecord> unsettledRecords = new();

        if (sessionKey != null)
        {
            unsettledRecords = await _dbContext.BetRecords
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
            var activeBonuses = await _dbContext.UzyteKodyBonusowe
                .Where(b => b.UserId == userId && b.Status == BonusStatus.Active)
                .OrderBy(b => b.Uzyto)
                .ToListAsync();

            // Zbierz dedukcje per-bonus ze wszystkich BetRecordów w tej sesji
            var perBonusDeducted = new Dictionary<int, decimal>();
            foreach (var record in unsettledRecords)
            {
                if (record.BonusDeductions != null)
                {
                    foreach (var part in record.BonusDeductions.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = part.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[0], out var bid) && decimal.TryParse(parts[1], out var amt))
                        {
                            perBonusDeducted.TryGetValue(bid, out var existing);
                            perBonusDeducted[bid] = existing + amt;
                        }
                    }
                }
            }

            if (perBonusDeducted.Count > 0)
            {
                // Proporcjonalny zwrot: każdy bonus dostaje udział wg swojego udziału w dedukcji
                var totalDeducted = perBonusDeducted.Values.Sum();
                foreach (var bonus in activeBonuses)
                {
                    if (perBonusDeducted.TryGetValue(bonus.Id, out var deducted))
                    {
                        var share = deducted / totalDeducted;
                        var addAmount = amountToBonus * share;
                        bonus.RemainingAmount += addAmount;
                    }
                }
            }
            else
            {
                // Fallback (stare rekordy bez BonusDeductions): FIFO — całość do najstarszego
                var remainingToAdd = amountToBonus;
                foreach (var bonus in activeBonuses)
                {
                    if (remainingToAdd <= 0) break;
                    bonus.RemainingAmount += remainingToAdd;
                    remainingToAdd = 0;
                }
            }

            user.BalanceBonus += amountToBonus;
        }

        if (amountToReal > 0)
        {
            user.BalanceReal += amountToReal;
        }

        await RefreshBalanceBonusAsync(userId);
        await _dbContext.SaveChangesAsync();

        var updatedUser = await _dbContext.Users.FindAsync(userId);
        return BalanceResult.Ok(updatedUser!.BalanceReal, updatedUser.BalanceBonus);
    }

    public async Task<BalanceResult> WithdrawAsync(int userId, decimal amount)
    {
        if (amount <= 0)
            return BalanceResult.Failed("Kwota wyplaty musi byc wieksza od zera.");

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return BalanceResult.Failed("Nie znaleziono uzytkownika.");

        await ExpireActiveBonusesAsync(userId);
        await RefreshBalanceBonusAsync(userId);
        user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return BalanceResult.Failed("Nie znaleziono uzytkownika.");

        if (user.BalanceBonus > 0)
        {
            await CheckAndConvertOrCancelBonusesAsync(userId);
            await RefreshBalanceBonusAsync(userId);
            user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
                return BalanceResult.Failed("Nie znaleziono uzytkownika.");
        }

        if (user.BalanceReal < amount)
            return BalanceResult.Failed("Brak wystarczajacych srodkow.", user.BalanceReal, user.BalanceBonus);

        user.BalanceReal -= amount;
        await _dbContext.SaveChangesAsync();

        return BalanceResult.Ok(user.BalanceReal, user.BalanceBonus);
    }

    public async Task<BalanceResult> CheckAndCancelBonusAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return BalanceResult.Failed("Nie znaleziono uzytkownika.");

        await ExpireActiveBonusesAsync(userId);
        await CheckAndConvertOrCancelBonusesAsync(userId);
        await RefreshBalanceBonusAsync(userId);
        await _dbContext.SaveChangesAsync();

        user = await _dbContext.Users.FindAsync(userId);
        return BalanceResult.Ok(user!.BalanceReal, user.BalanceBonus);
    }

    // ── POMOCNICZE ─────────────────────────────────────────────────

    /// <summary>Sprawdza wagering dla aktywnych bonusów.
    /// Te z spełnionym wageringiem konwertuje na real.
    /// Te z niespełnionym anuluje.</summary>
    private async Task CheckAndConvertOrCancelBonusesAsync(int userId)
    {
        var activeBonuses = await _dbContext.UzyteKodyBonusowe
            .Where(b => b.UserId == userId && b.Status == BonusStatus.Active)
            .ToListAsync();

        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null) return;

        foreach (var bonus in activeBonuses)
        {
            if (bonus.WageringProgress >= bonus.WageringRequired)
            {
                // Wagering spełniony — przenieś pozostałą kwotę na real
                    var convertAmount = bonus.RemainingAmount;
                    if (convertAmount > 0)
                    {
                        user.BalanceReal += convertAmount;
                        bonus.RemainingAmount = 0;
                    }
                bonus.Status = BonusStatus.WageringMet;
            }
            else
            {
                // Wagering niespełniony — anuluj bonus
                bonus.Status = BonusStatus.Cancelled;
                bonus.ExpiredAt = DateTime.UtcNow;
                bonus.RemainingAmount = 0;
            }
        }
    }

    /// <summary>Wygasza przeterminowane bonusy.</summary>
    private async Task ExpireActiveBonusesAsync(int userId)
    {
        var expiredBonuses = await _dbContext.UzyteKodyBonusowe
            .Where(b => b.UserId == userId
                && b.Status == BonusStatus.Active
                && b.ExpiresAt != null
                && b.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var bonus in expiredBonuses)
        {
            bonus.Status = BonusStatus.Expired;
            bonus.ExpiredAt = DateTime.UtcNow;
            bonus.RemainingAmount = 0;
        }

        if (expiredBonuses.Any())
            await _dbContext.SaveChangesAsync();
    }

    /// <summary>Odświeża User.BalanceBonus jako SUM RemainingAmount z aktywnych bonusów.</summary>
    private async Task RefreshBalanceBonusAsync(int userId)
    {
        var totalBonus = await _dbContext.UzyteKodyBonusowe
            .Where(b => b.UserId == userId && b.Status == BonusStatus.Active)
            .SumAsync(b => (decimal?)b.RemainingAmount) ?? 0m;

        await _dbContext.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.BalanceBonus, totalBonus));
    }
}

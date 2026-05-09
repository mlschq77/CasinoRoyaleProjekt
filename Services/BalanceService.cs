using CasinoRoyale.Data;
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
            .Select(user => (decimal?)user.Balance)
            .FirstOrDefaultAsync();
    }

    public async Task<BalanceResult> PlaceBetAsync(int userId, decimal amount)
    {
        if (amount <= 0)
            return BalanceResult.Failed("Stawka musi byc wieksza od zera.");

        var updatedRows = await _dbContext.Users
            .Where(user => user.Id == userId && user.Balance >= amount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.Balance, user => user.Balance - amount));

        var balance = await GetBalanceAsync(userId);

        if (balance == null)
            return BalanceResult.Failed("Nie znaleziono uzytkownika.");

        if (updatedRows == 0)
            return BalanceResult.Failed("Brak wystarczajacych srodkow.", balance.Value);

        return BalanceResult.Ok(balance.Value);
    }

    public async Task<BalanceResult> PayoutAsync(int userId, decimal amount)
    {
        if (amount < 0)
            return BalanceResult.Failed("Wyplata nie moze byc ujemna.");

        var updatedRows = await _dbContext.Users
            .Where(user => user.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.Balance, user => user.Balance + amount));

        var balance = await GetBalanceAsync(userId);

        if (updatedRows == 0 || balance == null)
            return BalanceResult.Failed("Nie znaleziono uzytkownika.");

        return BalanceResult.Ok(balance.Value);
    }
}
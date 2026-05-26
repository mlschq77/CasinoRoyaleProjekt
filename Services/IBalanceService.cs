namespace CasinoRoyale.Services;

public interface IBalanceService
{
    Task<decimal?> GetBalanceAsync(int userId);
    Task<BalanceInfo?> GetBalanceInfoAsync(int userId);
    Task<BalanceResult> PlaceBetAsync(int userId, decimal amount, string? sessionKey = null);
    Task<BalanceResult> PayoutAsync(int userId, decimal amount, string? sessionKey = null);
    Task<BalanceResult> WithdrawAsync(int userId, decimal amount);
    Task<BalanceResult> CheckAndCancelBonusAsync(int userId);
}

namespace CasinoRoyale.Services;

public class BalanceResult
{
    public bool Success { get; init; }
    public decimal Balance { get; init; }
    public string? Error { get; init; }

    public static BalanceResult Ok(decimal balance) => new()
    {
        Success = true,
        Balance = balance
    };

    public static BalanceResult Failed(string error, decimal balance = 0) => new()
    {
        Success = false,
        Balance = balance,
        Error = error
    };
}
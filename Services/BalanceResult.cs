namespace CasinoRoyale.Services;

public class BalanceResult
{
    public bool Success { get; init; }
    public decimal Balance { get; init; }

    /// <summary>Balans rzeczywisty po operacji.</summary>
    public decimal BalanceReal { get; init; }

    /// <summary>Balans bonusowy po operacji.</summary>
    public decimal BalanceBonus { get; init; }

    public decimal TotalBalance => BalanceReal + BalanceBonus;

    public string? Error { get; init; }

    /// <summary>Ile z zakładu zostało pobrane z bonusu (tylko dla PlaceBet).</summary>
    public decimal AmountFromBonus { get; init; }

    /// <summary>SessionKey wygenerowany lub przekazany dla zakładu (tylko dla PlaceBet).</summary>
    public string? SessionKey { get; init; }

    public static BalanceResult Ok(decimal balanceReal, decimal balanceBonus = 0, decimal amountFromBonus = 0, string? sessionKey = null) => new()
    {
        Success = true,
        Balance = balanceReal + balanceBonus,
        BalanceReal = balanceReal,
        BalanceBonus = balanceBonus,
        AmountFromBonus = amountFromBonus,
        SessionKey = sessionKey
    };

    public static BalanceResult Failed(string error, decimal balanceReal = 0, decimal balanceBonus = 0) => new()
    {
        Success = false,
        Balance = balanceReal + balanceBonus,
        BalanceReal = balanceReal,
        BalanceBonus = balanceBonus,
        Error = error
    };
}

namespace CasinoRoyale.Services;

public class BalanceInfo
{
    public decimal BalanceReal { get; set; }
    public decimal BalanceBonus { get; set; }
    public decimal TotalBalance => BalanceReal + BalanceBonus;

    /// <summary>Postęp wageringu dla wszystkich aktywnych bonusów (suma).</summary>
    public decimal WageringProgress { get; set; }

    /// <summary>Wymagany obrót dla wszystkich aktywnych bonusów (suma).</summary>
    public decimal WageringRequired { get; set; }

    public bool HasActiveBonus => BalanceBonus > 0;

    /// <summary>Czy wszystkie wymogi obrotu są spełnione.</summary>
    public bool IsWageringMet => !HasActiveBonus || WageringProgress >= WageringRequired;

    /// <summary>Data wygaśnięcia najstarszego aktywnego bonusu.</summary>
    public DateTime? ExpiresAt { get; set; }
}

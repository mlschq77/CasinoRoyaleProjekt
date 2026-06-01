namespace CasinoRoyale.Services;

public class BonusCodeApplicationResult
{
    public decimal BonusAmount { get; set; }
    public bool AlreadyUsed { get; set; }

    /// <summary>Wymagany obr�t dla tego bonusu.</summary>
    public decimal WageringRequired { get; set; }

    /// <summary>Data wyga�ni�cia bonusu.</summary>
    public DateTime? ExpiresAt { get; set; }
}

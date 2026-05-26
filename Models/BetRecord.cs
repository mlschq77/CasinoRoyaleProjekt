namespace CasinoRoyale.Models;

public class BetRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Pełna kwota zakładu.</summary>
    public decimal Amount { get; set; }

    /// <summary>Ile z tej kwoty pochodziło z bonusu (FIFO).</summary>
    public decimal AmountFromBonus { get; set; }

    /// <summary>Szczegółowe odliczenia per-bonus w formacie "BonusId:Amount;BonusId:Amount" np. "1:30;2:20".</summary>
    public string? BonusDeductions { get; set; }

    /// <summary>Klucz sesji gry np. "min:7", "csh:3", "bj:5", "pln:12".</summary>
    public string SessionKey { get; set; } = string.Empty;

    /// <summary>Czy payout został już rozliczony.</summary>
    public bool Settled { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

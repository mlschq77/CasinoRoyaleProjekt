namespace CasinoRoyale.Models;

public class UzytyKodBonusowy
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int KodBonusowyId { get; set; }
    public int? StripePaymentId { get; set; }
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Data u�ycia kodu / przyznania bonusu.</summary>
    public DateTime Uzyto { get; set; } = DateTime.UtcNow;

    // ── KWOTY ─────────────────────────────────────────────

    /// <summary>Ile przyznano bonusu (np. 50 PLN).</summary>
    public decimal BonusAmount { get; set; }

    /// <summary>Ile zosta�o z tego bonusu.</summary>
    public decimal RemainingAmount { get; set; }

    // ── WAGERING ──────────────────────────────────────────

    /// <summary>Wymagany obr�t (np. (wp�ata+bonus) * wageringMultiplier).</summary>
    public decimal WageringRequired { get; set; }

    /// <summary>Obecny post�p obrotu.</summary>
    public decimal WageringProgress { get; set; }

    // ── EXPIRY ────────────────────────────────────────────

    /// <summary>Moment wyga�ni�cia bonusu (Uzyto.AddDays(7)).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Kiedy faktycznie wygas� (null = wci�� wa�ny).</summary>
    public DateTime? ExpiredAt { get; set; }

    // ── STATUS ────────────────────────────────────────────

    public BonusStatus Status { get; set; } = BonusStatus.Active;
}

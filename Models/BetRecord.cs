namespace CasinoRoyale.Models;

public class BetRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountFromBonus { get; set; }
    public string? BonusDeductions { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public bool Settled { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Nazwa gry (np. "Blackjack", "Mines", "Fruits").</summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>Kwota wyplaty po zakonczeniu gry (null = gra jeszcze trwa).</summary>
    public decimal? PayoutAmount { get; set; }
}

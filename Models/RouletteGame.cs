namespace CasinoRoyale.Models;

public class RouletteGame
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal BetAmount { get; set; }
    public string BetType { get; set; } = string.Empty;   // "multi" gdy wiele zakładów
    public string BetValue { get; set; } = string.Empty;
    public int ResultNumber { get; set; }
    public decimal WinAmount { get; set; }
    public string? BetsJson { get; set; }                  // JSON z listą zakładów przy multi-bet
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

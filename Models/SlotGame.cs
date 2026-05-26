namespace CasinoRoyale.Models;

public class SlotGame
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal BetAmount { get; set; }
    public string SymbolsJson { get; set; } = string.Empty;
    public decimal Multiplier { get; set; }
    public decimal WinAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

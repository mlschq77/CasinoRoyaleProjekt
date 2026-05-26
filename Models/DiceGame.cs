namespace CasinoRoyale.Models;

public class DiceGame
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal BetAmount { get; set; }
    public string Mode { get; set; } = "over";
    public int Target { get; set; }
    public int Roll { get; set; }
    public decimal Multiplier { get; set; }
    public decimal WinAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

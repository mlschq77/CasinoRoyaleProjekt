namespace CasinoRoyale.Models;

public class PlinkoGame
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal BetAmount { get; set; }
    public string Risk { get; set; } = "medium";
    public decimal WinAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

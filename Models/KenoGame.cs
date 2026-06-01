namespace CasinoRoyale.Models;

public class KenoGame
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal BetAmount { get; set; }
    public string SelectedNumbers { get; set; } = string.Empty;
    public string DrawnNumbers { get; set; } = string.Empty;
    public int Hits { get; set; }
    public decimal Multiplier { get; set; }
    public decimal WinAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

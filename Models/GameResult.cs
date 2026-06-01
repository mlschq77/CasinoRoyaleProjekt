namespace CasinoRoyale.Models;

public class GameResult
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public decimal BetAmount { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal WinAmount { get; set; }
    public DateTime PlayedAtUtc { get; set; } = DateTime.UtcNow;
    public int? MinesGameId { get; set; }
}

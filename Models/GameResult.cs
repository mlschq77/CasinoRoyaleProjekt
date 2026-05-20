namespace CasinoRoyale.Models;

public class GameResult
{
    public int Id { get; set; }
    public int UserId { get; set; }
    /// <summary>Klucz gry w kolumnie GameKey (legacy).</summary>
    public string GameKey { get; set; } = string.Empty;
    /// <summary>To samo co GameKey — kolumna GameName w starszej bazie.</summary>
    public string GameName { get; set; } = string.Empty;
    public decimal BetAmount { get; set; }
    /// <summary>Wyplata z rundy (kolumna PayoutAmount).</summary>
    public decimal PayoutAmount { get; set; }
    /// <summary>Lustrzana wyplata w kolumnie WinAmount (legacy).</summary>
    public decimal WinAmount { get; set; }
    /// <summary>Czas rundy UTC (w bazie moze byc kolumna PlayedAt).</summary>
    public DateTime PlayedAtUtc { get; set; } = DateTime.UtcNow;
    public int? MinesGameId { get; set; }
}

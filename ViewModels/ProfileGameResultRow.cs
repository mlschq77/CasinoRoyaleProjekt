namespace CasinoRoyale.ViewModels;

public class ProfileGameResultRow
{
    public DateTime PlayedAtUtc { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public decimal BetAmount { get; set; }
    public decimal PayoutAmount { get; set; }

    public decimal NetAmount => PayoutAmount - BetAmount;
}

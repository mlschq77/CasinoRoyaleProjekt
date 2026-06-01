namespace CasinoRoyale.Models;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal BalanceReal { get; set; } = 0m;
    public decimal BalanceBonus { get; set; }
    public int? ActiveBonusId { get; set; }
    public decimal? WageringRequired { get; set; }
    public decimal? WageringProgress { get; set; }
    public DateTime? BonusExpiresAt { get; set; }
    public User User { get; set; } = null!;
}

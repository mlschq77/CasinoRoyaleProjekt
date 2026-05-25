namespace CasinoRoyale.Models;

public class UzytyKodBonusowy
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int KodBonusowyId { get; set; }
    public int? StripePaymentId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime Uzyto { get; set; } = DateTime.UtcNow;
}

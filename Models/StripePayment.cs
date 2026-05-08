namespace CasinoRoyale.Models;

public class StripePayment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "pln";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
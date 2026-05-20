namespace CasinoRoyale.Models;

public class StripeWithdrawal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TransferId { get; set; } = string.Empty;
    public string DestinationAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "pln";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

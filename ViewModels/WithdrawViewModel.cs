namespace CasinoRoyale.ViewModels;

public class WithdrawViewModel
{
    public decimal Amount { get; set; } = 100m;
    public string DestinationAccountId { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool StripeConfigured { get; set; }
}

namespace CasinoRoyale.ViewModels;

public class DepositViewModel
{
    public decimal Amount { get; set; } = 100m;
    public string? Message { get; set; }
    public bool StripeConfigured { get; set; }
}
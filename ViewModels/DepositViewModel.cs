using System.ComponentModel.DataAnnotations;
namespace CasinoRoyale.ViewModels;

public class DepositViewModel
{
    [Display(Name = "Kwota PLN")]
    public decimal Amount { get; set; } = 100m;

    [Display(Name = "Kod bonusowy")]
    public string? KodBonusowy { get; set; }

    public string? Message { get; set; }
    public bool StripeConfigured { get; set; }
}

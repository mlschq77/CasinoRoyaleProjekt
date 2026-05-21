using System.ComponentModel.DataAnnotations;
namespace CasinoRoyale.ViewModels;

public class DepositViewModel
{
    [Range(typeof(decimal), "10", "10000", ErrorMessage = "Kwota doladowania musi byc w zakresie 10-10000.")]
    [Display(Name = "Kwota PLN")]
    public decimal Amount { get; set; } = 100m;

    [StringLength(64, ErrorMessage = "Kod bonusowy moze miec maksymalnie 64 znaki.")]
    [RegularExpression(@"^[A-Za-z0-9_-]*$", ErrorMessage = "Kod bonusowy moze zawierac tylko litery, cyfry, myslnik i podkreslenie.")]
    [Display(Name = "Kod bonusowy")]
    public string? KodBonusowy { get; set; }

    public string? Message { get; set; }
    public bool StripeConfigured { get; set; }
}

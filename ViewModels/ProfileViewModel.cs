using System.ComponentModel.DataAnnotations;

namespace CasinoRoyale.ViewModels;

public class ProfileViewModel
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Imie jest wymagane.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Imie musi miec od 2 do 50 znakow.")]
    [Display(Name = "Imie")]
    public string Imie { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Nazwisko musi miec od 2 do 50 znakow.")]
    [Display(Name = "Nazwisko")]
    public string Nazwisko { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwa u�ytkownika jest wymagana.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Nazwa musi mie� od 3 do 50 znak�w.")]
    [Display(Name = "Nazwa u�ytkownika")]
    public string Nazwa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal BalanceBonus { get; set; }
    public DateTime DataRejestracji { get; set; }
    public List<TransactionHistoryItemViewModel> TransactionHistory { get; set; } = new();
    public List<BetHistoryItemViewModel> BetHistory { get; set; } = new();

    public string PelneImie
    {
        get
        {
            var pelneImie = $"{Imie} {Nazwisko}".Trim();
            return string.IsNullOrWhiteSpace(pelneImie) ? Nazwa : pelneImie;
        }
    }

    public string KycStatusDisplay { get; set; } = string.Empty;
    public string KycStatusCssClass { get; set; } = string.Empty;

    public string Inicjaly
    {
        get
        {
            var pierwsza = string.IsNullOrWhiteSpace(Imie) ? string.Empty : Imie[0].ToString();
            var druga = string.IsNullOrWhiteSpace(Nazwisko) ? string.Empty : Nazwisko[0].ToString();
            var inicjaly = $"{pierwsza}{druga}";

            return string.IsNullOrWhiteSpace(inicjaly)
                ? Nazwa[..Math.Min(Nazwa.Length, 2)].ToUpperInvariant()
                : inicjaly.ToUpperInvariant();
        }
    }
}

public class TransactionHistoryItemViewModel
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PLN";
    public DateTime CreatedAt { get; set; }
}

public class BetHistoryItemViewModel
{
    public string GameName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal BetAmount { get; set; }
}

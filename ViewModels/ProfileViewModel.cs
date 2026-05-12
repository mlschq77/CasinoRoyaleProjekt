namespace CasinoRoyale.ViewModels;

public class ProfileViewModel
{
    public int Id { get; set; }
    public string Imie { get; set; } = string.Empty;
    public string Nazwisko { get; set; } = string.Empty;
    public string Nazwa { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public decimal Balance { get; set; }
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

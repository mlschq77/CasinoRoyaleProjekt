namespace CasinoRoyale.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Imie { get; set; } = string.Empty;
        public string Nazwisko { get; set; } = string.Empty;
        public string Nazwa { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HasloHash { get; set; } = string.Empty;
        public decimal Balance { get; set; } = 1000m;
        public DateTime DataRejestracji { get; set; } = DateTime.UtcNow;
    }
}

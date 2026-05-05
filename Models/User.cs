namespace CasinoRoyale.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Nazwa { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        // Hasło jako BCrypt hash — nigdy plaintext
        public string HasloHash { get; set; } = string.Empty;
        public DateTime DataRejestracji { get; set; } = DateTime.UtcNow;
    }
}
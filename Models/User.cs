using System.ComponentModel.DataAnnotations;
namespace CasinoRoyale.Models
{
    public class User
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Imie jest wymagane.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Imie musi miec od 2 do 50 znakow.")]
        [Display(Name = "Imie")]
        public string Imie { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Nazwisko musi miec od 2 do 50 znakow.")]
        [Display(Name = "Nazwisko")]
        public string Nazwisko { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwa u¿ytkownika jest wymagana.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Nazwa musi mieæ od 3 do 50 znaków.")]
        [Display(Name = "Nazwa u¿ytkownika")]
        public string Nazwa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;
        public string HasloHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public decimal Balance { get; set; } = 1000m;
        public DateTime DataRejestracji { get; init; } = DateTime.UtcNow;
    }
}

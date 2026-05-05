using System.ComponentModel.DataAnnotations;

namespace CasinoRoyale.ViewModels
{
    public class RejestracjaViewModel
    {
        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Nazwa musi mieć od 3 do 50 znaków.")]
        [Display(Name = "Nazwa użytkownika")]
        public string Nazwa { get; set; } = string.Empty;

        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków.")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Haslo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]
        [DataType(DataType.Password)]
        [Compare("Haslo", ErrorMessage = "Hasła nie są identyczne.")]
        [Display(Name = "Potwierdź hasło")]
        public string PotwierdzHaslo { get; set; } = string.Empty;
    }

    public class LogowanieViewModel
    {
        [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Podaj poprawny adres e-mail.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Haslo { get; set; } = string.Empty;

        [Display(Name = "Zapamiętaj mnie")]
        public bool ZapamiętajMnie { get; set; }
    }
}
namespace CasinoRoyale.Models;

public class KodBonusowy
{
    public int Id { get; set; }
    public string Kod { get; set; } = string.Empty;
    public decimal MinimalnaWplata { get; set; }
    public decimal BonusProcentowy { get; set; }
    public decimal BonusKwotowy { get; set; }
    public DateTime Utworzono { get; set; } = DateTime.UtcNow;
    public DateTime? WaznyDo { get; set; }
}

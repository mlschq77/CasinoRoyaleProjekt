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

    /// <summary>Mnoznik wageringu (np. 35 = obrot x35). Domyslnie 20.</summary>
    public decimal WageringMultiplier { get; set; } = 20m;
}

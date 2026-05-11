using System.ComponentModel.DataAnnotations;

namespace CasinoRoyale.Models;

public class Kategoria
{
    [Required]
    public int Id { get; init; }

    [Required]
    public string Nazwa { get; init; } = string.Empty;

    public ICollection<AutomatInfo> Automaty { get; init; } = new List<AutomatInfo>();
}

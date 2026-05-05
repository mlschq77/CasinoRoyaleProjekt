using System.ComponentModel.DataAnnotations;

namespace CasinoRoyale.Models;

public class AutomatInfo
{

    [Required]
    public int Id { get; init; }

    [Required]
    public string Nazwa { get; init; } = string.Empty;

    [Required]
    public string Kategoria { get; init; } = string.Empty;
}

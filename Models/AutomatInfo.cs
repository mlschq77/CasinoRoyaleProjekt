using System.ComponentModel.DataAnnotations;

namespace CasinoRoyale.Models.ViewModels;

public class AutomatInfo
{

    [Required]
    public int Id { get; init; }

    [Required]
    public string Nazwa { get; init; } = string.Empty;

    [Required]
    public string Rodzaj { get; init; } = string.Empty;
}

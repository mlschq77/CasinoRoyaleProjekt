using System.ComponentModel.DataAnnotations;
namespace CasinoRoyale.Models;

public class AutomatInfo
{

    [Required]
    public int Id { get; init; }

    [Required]
    public string Nazwa { get; init; } = string.Empty;

    public int? ProviderId { get; init; }

    public AutomatProvider? Provider { get; init; }

    public ICollection<Kategoria> Kategorie { get; init; } = new List<Kategoria>();
}

using CasinoRoyale.Models;
namespace CasinoRoyale.ViewModels;

public class SlotsViewModel
{
    public IReadOnlyCollection<AutomatInfo> Gry { get; init; } = Array.Empty<AutomatInfo>();
    public IReadOnlyCollection<string> Kategorie { get; init; } = Array.Empty<string>();
    public string? WybranaKategoria { get; init; }
}

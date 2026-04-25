namespace CasinoRoyale.Models.ViewModels;

public class HomeIndexViewModel
{
    public IReadOnlyCollection<AutomatInfo> Gry { get; init; } = Array.Empty<AutomatInfo>();
}

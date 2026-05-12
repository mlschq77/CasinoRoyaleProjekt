using CasinoRoyale.Models;

namespace CasinoRoyale.ViewModels;

public class PromocjeViewModel
{
    public IReadOnlyList<KodBonusowy> Kody { get; init; } = Array.Empty<KodBonusowy>();
}

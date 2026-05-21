using CasinoRoyale.Models;

namespace CasinoRoyale.Services;

public class BonusCodeValidationResult
{
    public KodBonusowy? KodBonusowy { get; set; }
    public string? Error { get; set; }
    public decimal BonusAmount { get; set; }
    public bool AlreadyUsed { get; set; }

    public bool IsValid => Error == null;
    public bool HasCode => KodBonusowy != null;
}

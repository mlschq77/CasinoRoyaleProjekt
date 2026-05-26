using CasinoRoyale.Models;

namespace CasinoRoyale.ViewModels;

public class ProfileBonusesViewModel
{
    public decimal BalanceReal { get; set; }
    public decimal BalanceBonus { get; set; }
    public decimal TotalWageringProgress { get; set; }
    public decimal TotalWageringRequired { get; set; }
    public DateTime? EarliestExpiry { get; set; }

    public List<ActiveBonusViewModel> Bonuses { get; set; } = new();

    public bool HasActiveBonuses => Bonuses.Count > 0;

    public decimal TotalWageringPercent =>
        TotalWageringRequired > 0
            ? Math.Round(TotalWageringProgress / TotalWageringRequired * 100, 1)
            : 0;

    public bool IsWageringMet => TotalWageringProgress >= TotalWageringRequired;
}

public class ActiveBonusViewModel
{
    public string CodeName { get; set; } = string.Empty;
    public decimal BonusAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal WageringProgress { get; set; }
    public decimal WageringRequired { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public BonusStatus Status { get; set; }

    public string StatusDisplay => Status switch
    {
        BonusStatus.Active => "Aktywny",
        BonusStatus.WageringMet => "Obrot spelniomy",
        BonusStatus.Cancelled => "Anulowany",
        BonusStatus.Expired => "Wygasl",
        _ => "Nieznany"
    };

    public decimal WageringPercent =>
        WageringRequired > 0
            ? Math.Round(WageringProgress / WageringRequired * 100, 1)
            : 0;

    public string StatusCssClass => Status switch
    {
        BonusStatus.Active => "text-warning",
        BonusStatus.WageringMet => "text-success",
        BonusStatus.Cancelled => "text-danger",
        BonusStatus.Expired => "text-secondary",
        _ => "text-secondary"
    };

    public string ProgressBarCssClass => Status switch
    {
        BonusStatus.Active when WageringPercent >= 100 => "bg-success",
        BonusStatus.Active when WageringPercent >= 70 => "bg-info",
        BonusStatus.Active => "bg-warning",
        BonusStatus.WageringMet => "bg-success",
        _ => "bg-secondary"
    };

    public string ExpiryDisplay =>
        ExpiresAt.HasValue
            ? ExpiresAt.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
            : "—";
}

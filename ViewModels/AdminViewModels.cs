using CasinoRoyale.Models;

namespace CasinoRoyale.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public decimal TotalBalance { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public int ActiveBonusCodes { get; set; }
    public List<User> RecentUsers { get; set; } = new();
}

public class AdminStatystykiViewModel
{
    public int LiczbaBlackjack { get; set; }
    public int LiczbaMines { get; set; }
    public int LiczbaPlinko { get; set; }
    public int LiczbaDice { get; set; }
    public int LiczbaKeno { get; set; }
    public decimal SumaWplatBlackjack { get; set; }
    public decimal SumaWplatMines { get; set; }
    public decimal SumaWplatPlinko { get; set; }
    public decimal SumaWplatDice { get; set; }
    public decimal SumaWplatKeno { get; set; }
    public int LacznaLiczbaGier => LiczbaBlackjack + LiczbaMines + LiczbaPlinko + LiczbaDice + LiczbaKeno;
    public decimal LacznaSumaWplat => SumaWplatBlackjack + SumaWplatMines + SumaWplatPlinko + SumaWplatDice + SumaWplatKeno;
}

public class AdminKycViewModel
{
    public List<KycDocument> PendingDocuments { get; set; } = new();
    public List<KycDocument> AllDocuments { get; set; } = new();
    public Dictionary<int, string> UserEmails { get; set; } = new();
}

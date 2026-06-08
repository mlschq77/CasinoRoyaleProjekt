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
    public List<GameStatEntry> Gry { get; set; } = new();

    public int LacznaLiczbaGier => Gry.Sum(g => g.LiczbaGier);
    public decimal LacznaSumaWplat => Gry.Sum(g => g.SumaZakladow);
}

public class GameStatEntry
{
    public string GameKey { get; set; } = string.Empty;
    public string GameName { get; set; } = string.Empty;
    public int LiczbaGier { get; set; }
    public decimal SumaZakladow { get; set; }
}

public class AdminKycViewModel
{
    public List<KycDocument> PendingDocuments { get; set; } = new();
    public List<KycDocument> AllDocuments { get; set; } = new();
    public Dictionary<int, string> UserEmails { get; set; } = new();
}

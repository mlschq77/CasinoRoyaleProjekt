namespace CasinoRoyale.Models
{
    public class CrashSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public decimal BetAmount { get; set; }
        public decimal CrashPoint { get; set; } 
        public DateTime StartTime { get; set; } 

        public bool IsActive { get; set; } = true;
        public decimal? CashoutMultiplier { get; set; } 
        public decimal? WinAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

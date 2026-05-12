namespace CasinoRoyale.Models
{
    public class BlackjackGame
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string DeckJson { get; set; } = "";
        public string PlayerHandJson { get; set; } = "";
        public string DealerHandJson { get; set; } = "";
        public string SplitHandJson { get; set; } = "";

        public decimal BetAmount { get; set; }
        public decimal SplitBetAmount { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsPlayerTurn { get; set; } = true;
        public bool IsSplitActive { get; set; } = false;
        public bool IsPlayingSplitHand { get; set; } = false;
        public bool PayoutProcessed { get; set; } = false;

        public string Result { get; set; } = "";
        public decimal WinAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }
    }
}

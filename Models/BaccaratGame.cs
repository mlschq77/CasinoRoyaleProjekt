namespace CasinoRoyale.Models
{
    public class BaccaratGame
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string DeckJson { get; set; } = "";
        public string PlayerHandJson { get; set; } = "";
        public string BankerHandJson { get; set; } = "";

        public decimal BetAmount { get; set; }
        public string BetType { get; set; } = "";       // "player", "banker", "tie"

        public int PlayerValue { get; set; }
        public int BankerValue { get; set; }

        public string Result { get; set; } = "";        // "player_wins", "banker_wins", "tie"
        public decimal WinAmount { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

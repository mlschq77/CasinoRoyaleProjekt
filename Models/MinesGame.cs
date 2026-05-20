namespace CasinoRoyale.Models
{

    public class MinesGame
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public int GridSize { get; set; } = 25;
        public int MineCount { get; set; }

        public string MinePositions { get; set; } = "";
        public string RevealedPositions { get; set; } = "";

        public decimal BetAmount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}

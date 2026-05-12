namespace CasinoRoyale.Models
{
    public class BlackjackCard
    {
        public string Suit { get; set; } = "";
        public string Rank { get; set; } = "";
        public bool FaceDown { get; set; } = false;

        public int GetValue()
        {
            if (Rank == "A") return 11;
            if (Rank == "J" || Rank == "Q" || Rank == "K") return 10;
            return int.Parse(Rank);
        }
    }
}

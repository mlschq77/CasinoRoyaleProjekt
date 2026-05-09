namespace CasinoRoyale.Services
{

    public class MinesService
    {
        public List<int> GenerateMines(int mineCount)
        {
            var rand = new Random();
            var mines = new HashSet<int>();

            while (mines.Count < mineCount)
            {
                mines.Add(rand.Next(0, 25));
            }

            return mines.ToList();
        }

        public decimal CalculateMultiplier(int revealedCount)
        {
            return 1 + (revealedCount * 0.2m);
        }
    }

}//

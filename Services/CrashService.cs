namespace CasinoRoyale.Services
{
    public class CrashService
    {
        private const double GrowthRate = 0.06;

        public decimal CalculateMultiplier(TimeSpan timeElapsed)
        {
            double seconds = timeElapsed.TotalSeconds;
            double multiplier = Math.Exp(GrowthRate * seconds);
            return (decimal)Math.Round(multiplier, 2);
        }

        public decimal GenerateCrashPoint()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            var randomValue = BitConverter.ToUInt64(bytes, 0) / (double)ulong.MaxValue;

            var crashPoint = 1.0 / (1.0 - randomValue);

            if (crashPoint > 10000)
            {
                crashPoint = 10000;
            }

            if (crashPoint < 1.01)
            {
                crashPoint = 1.01;
            }

            return (decimal)Math.Round(crashPoint, 2);
        }

        public decimal CalculateWin(decimal betAmount, decimal multiplier)
        {
            return decimal.Round(betAmount * multiplier, 2);
        }
    }
}

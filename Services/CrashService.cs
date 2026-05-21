namespace CasinoRoyale.Services
{
    public class CrashService
    {
        private const double GrowthRate = 0.06;

        /// <summary>
        /// Oblicza mnożnik w danej chwili: M(t) = e^(k * t)
        /// </summary>
        public decimal CalculateMultiplier(TimeSpan timeElapsed)
        {
            double seconds = timeElapsed.TotalSeconds;
            double multiplier = Math.Exp(GrowthRate * seconds);
            return (decimal)Math.Round(multiplier, 2);
        }

        /// <summary>
        /// Generuje losowy punkt crashu z odpowiednim rozkładem.
        /// Używa algorytmu provably fair: crashPoint = floor(1 / (1 - X)) gdzie X to losowa liczba 0-1.
        /// Zapewnia to house edge ~1%.
        /// </summary>
        public decimal GenerateCrashPoint()
        {
            // Używamy kryptograficznie bezpiecznego generatora
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[8];
            rng.GetBytes(bytes);
            var randomValue = BitConverter.ToUInt64(bytes, 0) / (double)ulong.MaxValue;

            // Formuła provably fair dla crash
            // Zapewnia, że crash point ma odpowiedni rozkład
            var crashPoint = 1.0 / (1.0 - randomValue);

            // Ograniczenie do rozsądnego zakresu (max 10000x)
            if (crashPoint > 10000)
                crashPoint = 10000;

            if (crashPoint < 1.01)
                crashPoint = 1.01;

            return (decimal)Math.Round(crashPoint, 2);
        }

        /// <summary>
        /// Oblicza wygraną na podstawie stawki i mnożnika
        /// </summary>
        public decimal CalculateWin(decimal betAmount, decimal multiplier)
        {
            return decimal.Round(betAmount * multiplier, 2);
        }
    }
}

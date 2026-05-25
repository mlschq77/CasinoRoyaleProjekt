using System.Security.Cryptography;

namespace CasinoRoyale.Services;

public class RouletteService
{
    private static readonly HashSet<int> RedNumbers = new() { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };

    public int Spin() => RandomNumberGenerator.GetInt32(0, 37);

    public string GetColor(int number)
    {
        if (number == 0) return "green";
        return RedNumbers.Contains(number) ? "red" : "black";
    }

    public decimal CalculateWin(string betType, string betValue, int number, decimal bet)
    {
        bool wins = betType switch
        {
            "number"  => int.TryParse(betValue, out var n) && n == number,
            "red"     => RedNumbers.Contains(number),
            "black"   => number != 0 && !RedNumbers.Contains(number),
            "odd"     => number != 0 && number % 2 == 1,
            "even"    => number != 0 && number % 2 == 0,
            "low"     => number >= 1 && number <= 18,
            "high"    => number >= 19 && number <= 36,
            "dozen1"  => number >= 1 && number <= 12,
            "dozen2"  => number >= 13 && number <= 24,
            "dozen3"  => number >= 25 && number <= 36,
            "column1" => number != 0 && number % 3 == 1,
            "column2" => number != 0 && number % 3 == 2,
            "column3" => number != 0 && number % 3 == 0,
            _         => false
        };

        if (!wins) return 0;

        decimal multiplier = betType switch
        {
            "number"                                                                   => 36,
            "dozen1" or "dozen2" or "dozen3" or "column1" or "column2" or "column3" => 3,
            _                                                                          => 2
        };

        return decimal.Round(bet * multiplier, 2);
    }
}

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
        bool wins;
        decimal multiplier;

        switch (betType)
        {
            case "number":
                wins = int.TryParse(betValue, out var n) && n == number;
                multiplier = 36;
                break;

            // Split (2 numbers) — pays 17:1 → return 18× stake
            case "split":
            {
                var nums = ParseNumbers(betValue);
                wins = nums.Contains(number);
                multiplier = 18;
                break;
            }

            // Street / Trio (3 numbers) — pays 11:1 → return 12×
            case "street":
            {
                var nums = ParseNumbers(betValue);
                wins = nums.Contains(number);
                multiplier = 12;
                break;
            }

            // Corner / Carré (4 numbers) — pays 8:1 → return 9×
            case "corner":
            {
                var nums = ParseNumbers(betValue);
                wins = nums.Contains(number);
                multiplier = 9;
                break;
            }

            // Six-line / Sixain (6 numbers) — pays 5:1 → return 6×
            case "sixline":
            {
                var nums = ParseNumbers(betValue);
                wins = nums.Contains(number);
                multiplier = 6;
                break;
            }

            case "red":
                wins = RedNumbers.Contains(number);
                multiplier = 2;
                break;
            case "black":
                wins = number != 0 && !RedNumbers.Contains(number);
                multiplier = 2;
                break;
            case "odd":
                wins = number != 0 && number % 2 == 1;
                multiplier = 2;
                break;
            case "even":
                wins = number != 0 && number % 2 == 0;
                multiplier = 2;
                break;
            case "low":
                wins = number >= 1 && number <= 18;
                multiplier = 2;
                break;
            case "high":
                wins = number >= 19 && number <= 36;
                multiplier = 2;
                break;
            case "dozen1":
                wins = number >= 1 && number <= 12;
                multiplier = 3;
                break;
            case "dozen2":
                wins = number >= 13 && number <= 24;
                multiplier = 3;
                break;
            case "dozen3":
                wins = number >= 25 && number <= 36;
                multiplier = 3;
                break;
            case "column1":
                wins = number != 0 && number % 3 == 1;
                multiplier = 3;
                break;
            case "column2":
                wins = number != 0 && number % 3 == 2;
                multiplier = 3;
                break;
            case "column3":
                wins = number != 0 && number % 3 == 0;
                multiplier = 3;
                break;
            default:
                return 0;
        }

        if (!wins) return 0;
        return decimal.Round(bet * multiplier, 2);
    }

    private static HashSet<int> ParseNumbers(string value)
    {
        var result = new HashSet<int>();
        foreach (var part in value.Split('-'))
            if (int.TryParse(part, out var n))
                result.Add(n);
        return result;
    }
}

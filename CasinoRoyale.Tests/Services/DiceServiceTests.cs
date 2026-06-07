using CasinoRoyale.Services;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class DiceServiceTests
{
    private readonly DiceService _diceService;

    public DiceServiceTests()
    {
        _diceService = new DiceService();
    }

    [Theory]
    [InlineData(1, false)]  // Poniżej minimum
    [InlineData(2, true)]   // Minimum
    [InlineData(50, true)]  // Środek
    [InlineData(98, true)]  // Maksimum
    [InlineData(99, false)] // Powyżej maksimum
    public void IsValidTarget_ShouldValidateCorrectly(int target, bool expectedValidity)
    {
        
        var result = DiceService.IsValidTarget(target);

        
        Assert.Equal(expectedValidity, result);
    }

    [Theory]
    [InlineData("over", "over")]
    [InlineData(" OVER ", "over")]
    [InlineData("uNdEr", "under")]
    [InlineData("invalid", "over")]
    [InlineData(null, "over")]
    public void NormalizeMode_ShouldReturnStandardizedString(string? input, string expected)
    {
        
        var result = DiceService.NormalizeMode(input);

        
        Assert.Equal(expected, result);
    }

   
    [Theory]
    [InlineData(10, "over", 50, 50, 1.98)]   
    [InlineData(10, "under", 25, 24, 4.12)]  
    public void Play_ShouldCalculateChanceAndMultiplierCorrectly(
        decimal bet, string mode, int target, int expectedChance, decimal expectedMultiplier)
    {
        
        var result = _diceService.Play(bet, mode, target);

        
        Assert.Equal(expectedChance, result.Chance);
        Assert.Equal(expectedMultiplier, result.Multiplier);
    }
}
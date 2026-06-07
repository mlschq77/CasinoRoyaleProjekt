using CasinoRoyale.Services;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class RouletteServiceTests
{
    private readonly RouletteService _rouletteService;

    public RouletteServiceTests()
    {
        _rouletteService = new RouletteService();
    }

    [Theory]
    [InlineData(0, "green")]
    [InlineData(1, "red")]
    [InlineData(2, "black")]
    [InlineData(14, "red")]
    [InlineData(35, "black")]
    public void GetColor_ShouldReturnCorrectColor(int number, string expectedColor)
    {
        
        var color = _rouletteService.GetColor(number);

        
        Assert.Equal(expectedColor, color);
    }

    [Theory]
    [InlineData("number", "10", 10, 10, 360)]  
    [InlineData("number", "10", 15, 10, 0)]    
    [InlineData("red", "", 1, 10, 20)]         
    [InlineData("black", "", 1, 10, 0)]        
    [InlineData("even", "", 2, 10, 20)]       
    [InlineData("odd", "", 2, 10, 0)]          
    [InlineData("even", "", 0, 10, 0)]         
    [InlineData("dozen1", "", 12, 10, 30)]     
    [InlineData("dozen1", "", 13, 10, 0)]      
    public void CalculateWin_ShouldProcessBetsCorrectly(
        string betType, string betValue, int numberRolled, decimal betAmount, decimal expectedWin)
    {
        
        var win = _rouletteService.CalculateWin(betType, betValue, numberRolled, betAmount);

        
        Assert.Equal(expectedWin, win);
    }
}
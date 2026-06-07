using CasinoRoyale.Services;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class PlinkoServiceTests
{
    private readonly PlinkoService _plinkoService;

    public PlinkoServiceTests()
    {
        _plinkoService = new PlinkoService();
    }

    [Theory]
    [InlineData("low", 2.6)] 
    [InlineData("medium", 5.6)]
    [InlineData("high", 13.0)]
    [InlineData("INVALID_INPUT", 5.6)] 
    [InlineData(null, 5.6)] 
    public void GetMultipliers_ShouldReturnCorrectTable(string? risk, decimal expectedEdgeMultiplier)
    {
        
        var table = _plinkoService.GetMultipliers(risk!);

        
        Assert.Equal(9, table.Count); 
        Assert.Equal(expectedEdgeMultiplier, table[0]); 
    }
}
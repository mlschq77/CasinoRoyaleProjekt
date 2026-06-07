using CasinoRoyale.Services;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class MinesServiceTests
{
    private readonly MinesService _minesService;

    public MinesServiceTests()
    {
        
        _minesService = new MinesService();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(24)]
    public void GenerateMines_ShouldReturnExactNumberOfUniqueMines(int requestedMineCount)
    {
        
        var mines = _minesService.GenerateMines(requestedMineCount);

        
        Assert.Equal(requestedMineCount, mines.Count); 
        Assert.Equal(mines.Distinct().Count(), mines.Count); 
    }

    [Fact]
    public void GenerateMines_ShouldOnlyGenerateValidPositions()
    {
        
        int mineCount = 15;

        
        var mines = _minesService.GenerateMines(mineCount);

        
        Assert.All(mines, position =>
        {
            Assert.True(position >= 0 && position < 25, $"Pozycja {position} jest poza planszą!");
        });
    }

    [Theory]
    [InlineData(0, 1.0)]   // 0 odsłoniętych = mnożnik x1.0 (stawka bez zmian)
    [InlineData(1, 1.2)]   // 1 odsłonięte = mnożnik x1.2
    [InlineData(5, 2.0)]   // 5 odsłoniętych = mnożnik x2.0
    [InlineData(10, 3.0)]  // 10 odsłoniętych = mnożnik x3.0
    public void CalculateMultiplier_ShouldReturnCorrectValue(int revealedCount, decimal expectedMultiplier)
    {
        // Act
        var multiplier = _minesService.CalculateMultiplier(revealedCount);

        // Assert
        Assert.Equal(expectedMultiplier, multiplier);
    }
}
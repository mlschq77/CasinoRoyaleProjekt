using CasinoRoyale.Services;
using Xunit;
using System.Collections.Generic;

namespace CasinoRoyale.Tests.Services;

public class KenoServiceTests
{
    [Theory]
    [InlineData(new[] { 1, 2, 3 }, true)] // Poprawna ilość
    [InlineData(new[] { 1 }, true)] // Minimum
    [InlineData(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, true)] // Maksimum
    [InlineData(new int[0], false)] // Pusto
    [InlineData(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, false)] // Za dużo
    public void IsValidSelection_ShouldValidateCountCorrectly(int[] input, bool expected)
    {
        
        var result = KenoService.IsValidSelection(input);

        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void NormalizeNumbers_ShouldFilterSortAndDeduplicate()
    {
        
        var input = new[] { 15, -5, 41, 15, 30, 2, 0 };
        var expected = new List<int> { 2, 15, 30 }; 

        
        var result = KenoService.NormalizeNumbers(input);

        
        Assert.Equal(expected, result);
    }
}
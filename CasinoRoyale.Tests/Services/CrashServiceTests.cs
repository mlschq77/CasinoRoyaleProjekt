using System;
using CasinoRoyale.Services;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class CrashServiceTests
{
    private readonly CrashService _crashService;

    public CrashServiceTests()
    {
        _crashService = new CrashService();
    }

    [Fact]
    public void CalculateMultiplier_ShouldReturnBaseMultiplier_AtZeroSeconds()
    {
        
        var time = TimeSpan.Zero;

        
        var multiplier = _crashService.CalculateMultiplier(time);

        
        Assert.Equal(1.00m, multiplier); 
    }

    [Fact]
    public void CalculateMultiplier_ShouldIncreaseOverTime()
    {
        
        var time10Seconds = TimeSpan.FromSeconds(10);
        var time20Seconds = TimeSpan.FromSeconds(20);

        
        var mult10 = _crashService.CalculateMultiplier(time10Seconds);
        var mult20 = _crashService.CalculateMultiplier(time20Seconds);

        
        Assert.True(mult20 > mult10);
        
        Assert.Equal(1.82m, mult10);
    }

    [Fact]
    public void GenerateCrashPoint_ShouldBeWithinLimits()
    {
        
        for (int i = 0; i < 1000; i++)
        {
            var crashPoint = _crashService.GenerateCrashPoint();
            Assert.True(crashPoint >= 1.01m, "Crash point jest zbyt mały.");
            Assert.True(crashPoint <= 10000m, "Crash point przekroczył limit kasyna.");
        }
    }
}
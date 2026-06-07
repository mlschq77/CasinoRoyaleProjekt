using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class BaccaratServiceTests
{
    private readonly BaccaratService _baccaratService;

    public BaccaratServiceTests()
    {
        
        var options = new DbContextOptionsBuilder<Automaty>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new Automaty(options);
        var mockBalanceService = new Mock<IBonusCodeService>();
        var balanceService = new BalanceService(db, mockBalanceService.Object);

        _baccaratService = new BaccaratService(db, balanceService);
    }

    [Fact]
    public void CalculateHandValue_ShouldReturnCorrectModulo10Sum()
    {
        
        var hand = new List<BlackjackCard>
        {
            new BlackjackCard { Rank = "7" },
            new BlackjackCard { Rank = "8" }
        };

        
        int value = _baccaratService.CalculateHandValue(hand);

        
        Assert.Equal(5, value);
    }

    [Fact]
    public void CalculateHandValue_ShouldTreatFaceCardsAsZero()
    {
        
        var hand = new List<BlackjackCard>
        {
            new BlackjackCard { Rank = "K" }, 
            new BlackjackCard { Rank = "Q" }, 
            new BlackjackCard { Rank = "9" }  
        };

        
        int value = _baccaratService.CalculateHandValue(hand);

        
        Assert.Equal(9, value);
    }

    [Fact]
    public void CalculateHandValue_ShouldTreatAcesAsOne()
    {
        
        var hand = new List<BlackjackCard>
        {
            new BlackjackCard { Rank = "A" }, 
            new BlackjackCard { Rank = "A" }, 
            new BlackjackCard { Rank = "3" }  
        };

        
        int value = _baccaratService.CalculateHandValue(hand);

        
        Assert.Equal(5, value);
    }
}
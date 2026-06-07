using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class BlackjackServiceTests
{
    private readonly BlackjackService _blackjackService;

    public BlackjackServiceTests()
    {
        var options = new DbContextOptionsBuilder<Automaty>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new Automaty(options);
        var mockBalanceService = new Mock<IBonusCodeService>();
        var balanceService = new BalanceService(db, mockBalanceService.Object);

        _blackjackService = new BlackjackService(db, balanceService);
    }

    [Fact]
    public void CalculateHandValue_ShouldCountFaceCardsAsTen()
    {
        
        var hand = new List<BlackjackCard>
        {
            new BlackjackCard { Rank = "J" }, 
            new BlackjackCard { Rank = "K" }  
        };

        
        int value = _blackjackService.CalculateHandValue(hand);

        
        Assert.Equal(20, value);
    }

    [Fact]
    public void CalculateHandValue_ShouldCountAceAsEleven_WhenTotalDoesNotExceed21()
    {
        
        var hand = new List<BlackjackCard>
        {
            new BlackjackCard { Rank = "A" }, 
            new BlackjackCard { Rank = "9" }  
        };

        
        int value = _blackjackService.CalculateHandValue(hand);

        
        Assert.Equal(20, value);
    }

    [Fact]
    public void CalculateHandValue_ShouldCountAceAsOne_WhenTotalWouldExceed21()
    {
        
        var hand = new List<BlackjackCard>
        {
            new BlackjackCard { Rank = "A" }, 
            new BlackjackCard { Rank = "9" }, 
            new BlackjackCard { Rank = "8" }  
        };

        
        int value = _blackjackService.CalculateHandValue(hand);

        
        Assert.Equal(18, value); 
    }

    [Fact]
    public void CalculateHandValue_ShouldIgnoreFaceDownCards()
    {
        
        var hand = new List<BlackjackCard>
        {
            new BlackjackCard { Rank = "10", FaceDown = false }, 
            new BlackjackCard { Rank = "A", FaceDown = true }    
        };

        
        int value = _blackjackService.CalculateHandValue(hand);

        
        Assert.Equal(10, value);
    }
}
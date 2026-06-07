using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class BonusCodeServiceTests
{
    private Automaty GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<Automaty>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new Automaty(options);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenCodeIsInvalid()
    {
        
        using var dbContext = GetInMemoryDbContext();
        var service = new BonusCodeService(dbContext);

        

        
        var resultTooLong = await service.ValidateAsync(1, new string('A', 65), 100m);
        Assert.False(resultTooLong.IsValid);
        Assert.Contains("64", resultTooLong.Error);

        
        var resultBadChars = await service.ValidateAsync(1, "KOD!@#", 100m);
        Assert.False(resultBadChars.IsValid);
        Assert.Contains("tylko litery", resultBadChars.Error);

        
        var resultNotExist = await service.ValidateAsync(1, "NIEZNAJOMY", 100m);
        Assert.False(resultNotExist.IsValid);
        Assert.Contains("nie istnieje", resultNotExist.Error);
    }

    [Fact]
    public async Task ValidateAsync_ShouldCalculateCorrectBonusAmount()
    {
        
        using var dbContext = GetInMemoryDbContext();
        dbContext.KodyBonusowe.Add(new KodBonusowy
        {
            Kod = "START50",
            BonusProcentowy = 50, 
            BonusKwotowy = 20,   
            MinimalnaWplata = 50,
            WaznyDo = DateTime.UtcNow.AddDays(5)
        });
        await dbContext.SaveChangesAsync();

        var service = new BonusCodeService(dbContext);

        
        var result = await service.ValidateAsync(1, "START50", 200m);

        
        Assert.True(result.IsValid);
        Assert.NotNull(result.KodBonusowy);

        
        Assert.Equal(120m, result.BonusAmount);
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenDepositIsBelowMinimum()
    {
        
        using var dbContext = GetInMemoryDbContext();
        dbContext.KodyBonusowe.Add(new KodBonusowy
        {
            Kod = "MIN100",
            MinimalnaWplata = 100, 
            WaznyDo = DateTime.UtcNow.AddDays(5)
        });
        await dbContext.SaveChangesAsync();

        var service = new BonusCodeService(dbContext);

        
        var result = await service.ValidateAsync(1, "MIN100", 50m);

        
        Assert.False(result.IsValid);
        Assert.Contains("wymaga minimalnej wplaty", result.Error);
    }

    [Fact]
    public async Task TrackWageringProgressAsync_ShouldNotConvert_WhenWageringNotMet()
    {
        
        var service = new BonusCodeService(null!);
        var wallet = new Wallet
        {
            BalanceReal = 10m,
            BalanceBonus = 100m,
            WageringRequired = 500m,
            WageringProgress = 400m,
            ActiveBonusId = 1
        };

        
        await service.TrackWageringProgressAsync(wallet, 50m);

        
        Assert.Equal(450m, wallet.WageringProgress);
        Assert.Equal(100m, wallet.BalanceBonus); 
        Assert.Equal(10m, wallet.BalanceReal);   
        Assert.NotNull(wallet.ActiveBonusId);    
    }

    [Fact]
    public async Task TrackWageringProgressAsync_ShouldConvertToReal_WhenWageringIsMet()
    {
        // Arrange
        var service = new BonusCodeService(null!);
        var wallet = new Wallet
        {
            BalanceReal = 10m,
            BalanceBonus = 100m, 
            WageringRequired = 500m,
            WageringProgress = 450m,
            ActiveBonusId = 1
        };

        
        await service.TrackWageringProgressAsync(wallet, 60m);

        
        Assert.Equal(110m, wallet.BalanceReal);  
        Assert.Equal(0m, wallet.BalanceBonus);   
        Assert.Null(wallet.ActiveBonusId);       
        Assert.Null(wallet.WageringProgress);
        Assert.Null(wallet.WageringRequired);
    }

    [Fact]
    public async Task ExpireActiveBonusIfNeededAsync_ShouldClearBonus_WhenExpired()
    {
        
        var service = new BonusCodeService(null!);
        var wallet = new Wallet
        {
            BalanceReal = 50m,
            BalanceBonus = 200m, 
            ActiveBonusId = 1,
            BonusExpiresAt = DateTime.UtcNow.AddMinutes(-10) 
        };

        
        await service.ExpireActiveBonusIfNeededAsync(wallet);

        
        Assert.Equal(50m, wallet.BalanceReal); 
        Assert.Equal(0m, wallet.BalanceBonus); 
        Assert.Null(wallet.ActiveBonusId);
    }
}
using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CasinoRoyale.Tests.Services;

public class BalanceServiceTests
{
	
	private Automaty GetInMemoryDbContext()
	{
		var options = new DbContextOptionsBuilder<Automaty>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
			.Options;

		return new Automaty(options);
	}

	[Fact]
	public async Task PlaceBetAsync_ShouldReturnFailed_WhenAmountIsZeroOrLess()
	{
		
		using var dbContext = GetInMemoryDbContext();
		var mockBonusService = new Mock<IBonusCodeService>();
		var balanceService = new BalanceService(dbContext, mockBonusService.Object);

		
		var result = await balanceService.PlaceBetAsync(1, 0); 

		
		Assert.False(result.Success);
		Assert.Equal("Stawka musi byc wieksza od zera.", result.Error);
	}

	[Fact]
	public async Task PlaceBetAsync_ShouldReturnFailed_WhenNotEnoughFunds()
	{
		
		using var dbContext = GetInMemoryDbContext();
		var mockBonusService = new Mock<IBonusCodeService>();

		
		dbContext.Wallets.Add(new Wallet { UserId = 1, BalanceReal = 10m, BalanceBonus = 0m });
		await dbContext.SaveChangesAsync();

		var balanceService = new BalanceService(dbContext, mockBonusService.Object);

		
		var result = await balanceService.PlaceBetAsync(1, 50m);

		
		Assert.False(result.Success);
		Assert.Equal("Brak wystarczajacych srodkow.", result.Error);
	}

	[Fact]
	public async Task PlaceBetAsync_ShouldDeductFromRealBalance_WhenFundsAreSufficient()
	{
		
		using var dbContext = GetInMemoryDbContext();
		var mockBonusService = new Mock<IBonusCodeService>();

		dbContext.Wallets.Add(new Wallet { UserId = 2, BalanceReal = 100m, BalanceBonus = 0m });
		await dbContext.SaveChangesAsync();

		var balanceService = new BalanceService(dbContext, mockBonusService.Object);

		
		var result = await balanceService.PlaceBetAsync(2, 20m);

		
		Assert.True(result.Success);
		Assert.Equal(80m, result.BalanceReal); 

		
		var betRecord = await dbContext.BetRecords.FirstOrDefaultAsync(b => b.UserId == 2);
		Assert.NotNull(betRecord);
		Assert.Equal(20m, betRecord.Amount);
	}
}
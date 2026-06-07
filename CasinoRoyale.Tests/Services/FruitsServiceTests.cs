using CasinoRoyale.Services;
using Xunit;
using System.Linq;

namespace CasinoRoyale.Tests.Services;

public class FruitsServiceTests
{
    private readonly FruitsService _fruitsService;

    public FruitsServiceTests()
    {
        _fruitsService = new FruitsService();
    }

    [Fact]
    public void Spin_ShouldReturnExactlyFiveReels()
    {
       
        var result = _fruitsService.Spin(10m);

        
        Assert.NotNull(result);
        Assert.Equal(5, result.Reels.Count); 
    }

    [Fact]
    public void GetPaytable_ShouldReturnAllSymbols()
    {
        
        var paytable = _fruitsService.GetPaytable();

        
        Assert.Equal(6, paytable.Count); 

        
        var sevens = paytable.First(p => p.Name == "Siodemki");
        Assert.Equal(20m, sevens.Five);
    }
}
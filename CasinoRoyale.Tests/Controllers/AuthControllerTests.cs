using CasinoRoyale.Controllers;
using CasinoRoyale.Data;
using CasinoRoyale.Models;
using CasinoRoyale.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CasinoRoyale.Tests.Controllers;

public class AuthControllerTests
{
    private Automaty GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<Automaty>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new Automaty(options);
    }

    [Fact]
    public async Task Rejestracja_ShouldReturnViewWithError_WhenEmailIsTaken()
    {
        
        using var dbContext = GetInMemoryDbContext();

        
        dbContext.Users.Add(new User
        {
            Imie = "Jan",
            Nazwisko = "Kowalski",
            Nazwa = "JanK",
            Email = "zajety@email.com",
            HasloHash = "hash"
        });
        await dbContext.SaveChangesAsync();

        var authController = new AuthController(dbContext);

        
        var requestModel = new RejestracjaViewModel
        {
            Imie = "Anna",
            Nazwisko = "Nowak",
            Nazwa = "AnnaN",
            Email = "zajety@email.com", 
            Haslo = "Test1234!"
        };

       
        var result = await authController.Rejestracja(requestModel);

       
        
        var viewResult = Assert.IsType<ViewResult>(result);

        
        Assert.False(authController.ModelState.IsValid);
        Assert.True(authController.ModelState.ContainsKey("Email"));
    }
}
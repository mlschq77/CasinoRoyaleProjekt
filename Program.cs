using CasinoRoyale.Data;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();



        builder.Services.AddDbContext<Automaty>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));




        // ↓ NOWE — rejestracja cookie auth
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Logowanie";
                options.LogoutPath = "/Auth/Wylogowanie";
                options.ExpireTimeSpan = TimeSpan.FromHours(2);
                options.Cookie.Name = "CasinoRoyale.Auth";
                options.Cookie.HttpOnly = true;
            });

        builder.Services.AddScoped<MinesService>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication(); // ← NOWA LINIA (przed UseAuthorization!)
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "provably-fair",
            pattern: "provably-fair",
            defaults: new { controller = "Home", action = "ProvablyFair" });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
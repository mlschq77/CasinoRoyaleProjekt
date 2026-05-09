using CasinoRoyale.Data;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace CasinoRoyale;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();

        builder.Services.AddDbContext<Automaty>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure()
            ));



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
        builder.Services.AddScoped<IBalanceService, BalanceService>();

        var app = builder.Build();

        ApplyDatabaseMigrations(app);

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        
        app.UseStaticFiles();
        app.UseRouting();

        app.UseAuthentication(); 
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

    private static void ApplyDatabaseMigrations(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<Automaty>();

        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                dbContext.Database.Migrate();
                return;
            }
            catch (SqlException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database is not ready yet. Retrying migration attempt {Attempt}/{MaxAttempts}.", attempt, maxAttempts);
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }
}

using CasinoRoyale.Data;
using CasinoRoyale.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;

namespace CasinoRoyale;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Casino Royale API", Version = "v1" });
            c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
            c.IgnoreObsoleteActions();
        });

        builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

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

        builder.Services.Configure<RandomOrgOptions>(
            builder.Configuration.GetSection(RandomOrgOptions.SectionName));
        builder.Services.AddHttpClient<IRandomNumberService, RandomOrgRandomNumberService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RandomOrgOptions>>().Value;
            client.BaseAddress = new Uri(options.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        builder.Services.AddScoped<MinesService>();
        builder.Services.AddScoped<PlinkoService>();
        builder.Services.AddScoped<FruitsService>();
        builder.Services.AddScoped<BlackjackService>();
        builder.Services.AddScoped<CrashService>();
        builder.Services.AddScoped<RouletteService>();
        builder.Services.AddScoped<DiceService>();
        builder.Services.AddScoped<KenoService>();
        builder.Services.AddScoped<BaccaratService>();
        builder.Services.AddScoped<IBalanceService, BalanceService>();
        builder.Services.AddScoped<IBonusCodeService, BonusCodeService>();
        builder.Services.AddScoped<IKycService, KycService>();

        var app = builder.Build();

        ApplyDatabaseMigrations(app);
        if (SeedDataIfRequested(app, args))
        {
            return;
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }
        else
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Casino Royale API v1");
                c.RoutePrefix = "swagger";
            });
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
            name: "promocje",
            pattern: "promocje",
            defaults: new { controller = "Home", action = "Promocje" });

        app.MapControllerRoute(
            name: "regulamin",
            pattern: "regulamin",
            defaults: new { controller = "Home", action = "Regulamin" });

        app.MapControllerRoute(
            name: "kyc-upload",
            pattern: "kyc/{action=Index}/{id?}",
            defaults: new { controller = "Kyc" });

        app.MapControllerRoute(
            name: "kyc-info",
            pattern: "kyc/informacje",
            defaults: new { controller = "Home", action = "Kyc" });

        app.MapControllerRoute(
            name: "grajodpowiedzialnie",
            pattern: "grajodpowiedzialnie",
            defaults: new { controller = "Home", action = "GrajOdpowiedzialnie" });

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

    private static bool SeedDataIfRequested(WebApplication app, string[] args)
    {
        var seedEverything = args.Contains("--seed-fake-data");
        var seedUsers = args.Contains("--seed-users");
        var seedGames = args.Contains("--seed-games") || args.Contains("--seed-catalog");

        if (!seedEverything && !seedUsers && !seedGames)
        {
            return false;
        }

        var userCount = GetFakeUserCount(args);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Automaty>();

        if (seedEverything)
        {
            FakeDataSeeder.SeedAsync(dbContext, userCount).GetAwaiter().GetResult();
            return true;
        }

        if (seedGames)
        {
            FakeDataSeeder.SeedProvidersGamesAndCategoriesAsync(dbContext).GetAwaiter().GetResult();
        }

        if (seedUsers)
        {
            FakeDataSeeder.SeedUsersAsync(dbContext, userCount).GetAwaiter().GetResult();
        }

        return true;
    }

    private static int GetFakeUserCount(string[] args)
    {
        var countArg = args.FirstOrDefault(arg => arg.StartsWith("--fake-users=", StringComparison.OrdinalIgnoreCase));

        if (countArg is null)
        {
            return 25;
        }

        return int.TryParse(countArg["--fake-users=".Length..], out var count)
            ? count
            : 25;
    }
}


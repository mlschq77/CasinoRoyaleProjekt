using CasinoRoyale.Data;
using Microsoft.EntityFrameworkCore;

namespace CasinoRoyale;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddDbContext<Automaty>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("AutomatyConnection")));

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

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

using CasinoRoyale.Models;
using Microsoft.EntityFrameworkCore;
namespace CasinoRoyale.Data
{
    public class Automaty : DbContext
    {
        public Automaty(DbContextOptions<Automaty> options) :
            base(options)
        { }

        public DbSet<AutomatInfo> AutomatyInfo { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<MinesGame> MinesGames { get; set; }
        public DbSet<StripePayment> StripePayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .Property(user => user.Balance)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MinesGame>()
                .Property(game => game.BetAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<StripePayment>()
                .Property(payment => payment.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<StripePayment>()
                .HasIndex(payment => payment.SessionId)
                .IsUnique();
        }
    }
}

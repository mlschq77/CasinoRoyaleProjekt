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
        public DbSet<Kategoria> Kategorie { get; set; }
        public DbSet<AutomatProvider> AutomatProviderzy { get; set; }
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

            modelBuilder.Entity<Kategoria>()
                .ToTable("Kategorie");

            modelBuilder.Entity<Kategoria>()
                .Property(kategoria => kategoria.Nazwa)
                .HasMaxLength(450);

            modelBuilder.Entity<Kategoria>()
                .HasIndex(kategoria => kategoria.Nazwa)
                .IsUnique();

            modelBuilder.Entity<AutomatProvider>()
                .ToTable("AutomatProviderzy");

            modelBuilder.Entity<AutomatProvider>()
                .Property(provider => provider.Nazwa)
                .HasMaxLength(450);

            modelBuilder.Entity<AutomatProvider>()
                .HasIndex(provider => provider.Nazwa)
                .IsUnique();

            modelBuilder.Entity<AutomatInfo>()
                .HasOne(automat => automat.Provider)
                .WithMany(provider => provider.Automaty)
                .HasForeignKey(automat => automat.ProviderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AutomatInfo>()
                .HasMany(automat => automat.Kategorie)
                .WithMany(kategoria => kategoria.Automaty)
                .UsingEntity<Dictionary<string, object>>(
                    "AutomatyKategorie",
                    right => right
                        .HasOne<Kategoria>()
                        .WithMany()
                        .HasForeignKey("KategoriaId")
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left
                        .HasOne<AutomatInfo>()
                        .WithMany()
                        .HasForeignKey("AutomatId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("AutomatId", "KategoriaId");
                        join.ToTable("AutomatyKategorie");
                    });
        }
    }
}

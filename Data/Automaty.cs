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
        public DbSet<PlinkoGame> PlinkoGames { get; set; }
        public DbSet<StripePayment> StripePayments { get; set; }
        public DbSet<StripeWithdrawal> StripeWithdrawals { get; set; }
        public DbSet<KodBonusowy> KodyBonusowe { get; set; }
        public DbSet<UzytyKodBonusowy> UzyteKodyBonusowe { get; set; }
        public DbSet<BlackjackGame> BlackjackGames { get; set; }
        public DbSet<CrashSession> CrashSessions { get; set; }
        public DbSet<KycDocument> KycDocuments { get; set; }
        public DbSet<RouletteGame> RouletteGames { get; set; }
        public DbSet<BetRecord> BetRecords { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<DiceGame> DiceGames { get; set; }
        public DbSet<KenoGame> KenoGames { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Wallet ─────────────────────────────────────────
            modelBuilder.Entity<Wallet>()
                .ToTable("Wallets");

            modelBuilder.Entity<Wallet>()
                .Property(w => w.BalanceReal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Wallet>()
                .Property(w => w.BalanceBonus)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.User)
                .WithOne(u => u.Wallet)
                .HasForeignKey<Wallet>(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wallet>()
                .Property(w => w.WageringRequired)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Wallet>()
                .Property(w => w.WageringProgress)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Wallet>()
                .HasIndex(w => w.UserId)
                .IsUnique();

            // ── MinesGame ─────────────────────────────────────
            modelBuilder.Entity<MinesGame>()
                .Property(game => game.BetAmount)
                .HasColumnType("decimal(18,2)");

            // ── PlinkoGame ────────────────────────────────────
            modelBuilder.Entity<PlinkoGame>()
                .Property(game => game.BetAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PlinkoGame>()
                .Property(game => game.WinAmount)
                .HasColumnType("decimal(18,2)");

            // ── CrashSession ──────────────────────────────────
            modelBuilder.Entity<CrashSession>()
                .Property(session => session.BetAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CrashSession>()
                .Property(session => session.CrashPoint)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CrashSession>()
                .Property(session => session.CashoutMultiplier)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CrashSession>()
                .Property(session => session.WinAmount)
                .HasColumnType("decimal(18,2)");

            // ── StripePayment ─────────────────────────────────
            modelBuilder.Entity<StripePayment>()
                .Property(payment => payment.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<StripePayment>()
                .HasIndex(payment => payment.SessionId)
                .IsUnique();

            // ── KodBonusowy ───────────────────────────────────
            modelBuilder.Entity<KodBonusowy>()
                .ToTable("KodyBonusowe");

            modelBuilder.Entity<KodBonusowy>()
                .Property(kodBonusowy => kodBonusowy.Kod)
                .HasMaxLength(64);

            modelBuilder.Entity<KodBonusowy>()
                .Property(kodBonusowy => kodBonusowy.MinimalnaWplata)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<KodBonusowy>()
                .Property(kodBonusowy => kodBonusowy.BonusProcentowy)
                .HasColumnType("decimal(5,2)");

            modelBuilder.Entity<KodBonusowy>()
                .Property(kodBonusowy => kodBonusowy.BonusKwotowy)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<KodBonusowy>()
                .Property(kodBonusowy => kodBonusowy.WageringMultiplier)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<KodBonusowy>()
                .HasIndex(kodBonusowy => kodBonusowy.Kod)
                .IsUnique();

            // ── UzytyKodBonusowy ──────────────────────────────
            modelBuilder.Entity<UzytyKodBonusowy>()
                .ToTable("UzyteKodyBonusowe");

            modelBuilder.Entity<UzytyKodBonusowy>()
                .Property(uzytyKod => uzytyKod.SessionId)
                .HasMaxLength(450);

            modelBuilder.Entity<UzytyKodBonusowy>()
                .Property(b => b.BonusAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<UzytyKodBonusowy>()
                .HasIndex(uzytyKod => new { uzytyKod.UserId, uzytyKod.KodBonusowyId })
                .IsUnique();

            modelBuilder.Entity<UzytyKodBonusowy>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(uzytyKod => uzytyKod.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UzytyKodBonusowy>()
                .HasOne<KodBonusowy>()
                .WithMany()
                .HasForeignKey(uzytyKod => uzytyKod.KodBonusowyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UzytyKodBonusowy>()
                .HasOne<StripePayment>()
                .WithMany()
                .HasForeignKey(uzytyKod => uzytyKod.StripePaymentId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Kategoria ─────────────────────────────────────
            modelBuilder.Entity<Kategoria>()
                .ToTable("Kategorie");

            modelBuilder.Entity<RouletteGame>()
                .Property(g => g.BetAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<RouletteGame>()
                .Property(g => g.WinAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DiceGame>()
                .Property(game => game.BetAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DiceGame>()
                .Property(game => game.Multiplier)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<DiceGame>()
                .Property(game => game.WinAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<KenoGame>()
                .Property(game => game.BetAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<KenoGame>()
                .Property(game => game.Multiplier)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<KenoGame>()
                .Property(game => game.WinAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<StripeWithdrawal>()
                .Property(withdrawal => withdrawal.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<StripeWithdrawal>()
                .HasIndex(withdrawal => withdrawal.TransferId)
                .IsUnique();

            // ── AutomatProvider ──────────────────────────────
            modelBuilder.Entity<AutomatProvider>()
                .ToTable("AutomatProviderzy");

            modelBuilder.Entity<AutomatProvider>()
                .Property(provider => provider.Nazwa)
                .HasMaxLength(450);

            modelBuilder.Entity<AutomatProvider>()
                .HasIndex(provider => provider.Nazwa)
                .IsUnique();

            // ── BetRecord ────────────────────────────────────
            modelBuilder.Entity<BetRecord>()
                .Property(r => r.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<BetRecord>()
                .Property(r => r.AmountFromBonus)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<BetRecord>()
                .Property(r => r.SessionKey)
                .HasMaxLength(100);

            modelBuilder.Entity<BetRecord>()
                .Property(r => r.BonusDeductions)
                .HasMaxLength(500);

            modelBuilder.Entity<BetRecord>()
                .HasIndex(r => new { r.UserId, r.SessionKey, r.Settled });

            // ── LoginHistory ────────────────────────────────
            modelBuilder.Entity<LoginHistory>()
                .ToTable("LoginHistories");

            modelBuilder.Entity<LoginHistory>()
                .Property(h => h.IpAddress)
                .HasMaxLength(45);

            modelBuilder.Entity<LoginHistory>()
                .Property(h => h.UserAgent)
                .HasMaxLength(500);

            modelBuilder.Entity<LoginHistory>()
                .Property(h => h.EventType)
                .HasMaxLength(20);

            modelBuilder.Entity<LoginHistory>()
                .HasIndex(h => new { h.UserId, h.LoggedAt });


            // ── AutomatInfo ───────────────────────────────────
            modelBuilder.Entity<AutomatInfo>()
                .HasOne(automat => automat.Provider)
                .WithMany(provider => provider.Automaty)
                .HasForeignKey(automat => automat.ProviderId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AutomatInfo>()
                .HasMany(automat => automat.Kategorie)
                .WithMany(kategoria => kategoria.Automaty)
                .UsingEntity<AutomatKategoria>(
                    join => join
                        .HasOne(automatKategoria => automatKategoria.Kategoria)
                        .WithMany()
                        .HasForeignKey(automatKategoria => automatKategoria.KategoriaId)
                        .OnDelete(DeleteBehavior.Cascade),
                    join => join
                        .HasOne(automatKategoria => automatKategoria.Automat)
                        .WithMany()
                        .HasForeignKey(automatKategoria => automatKategoria.AutomatId)
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey(automatKategoria => new
                        {
                            automatKategoria.AutomatId,
                            automatKategoria.KategoriaId
                        });
                        join.ToTable("AutomatyKategorie");
                    });
        }
    }
}


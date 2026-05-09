using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CasinoRoyale.Data
{
    public class AutomatyFactory : IDesignTimeDbContextFactory<Automaty>
    {
        public Automaty CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<Automaty>();

            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=CasinoDB;User Id=sa;Password=Haslo123!;Encrypt=False;TrustServerCertificate=True;"
            );

            return new Automaty(optionsBuilder.Options);
        }
    }
}
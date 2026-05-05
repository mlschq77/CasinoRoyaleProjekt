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
    }
}

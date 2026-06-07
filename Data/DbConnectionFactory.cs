using Microsoft.Data.SqlClient;

namespace CasinoRoyale.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
    SqlConnection CreateAdminConnection();
    SqlConnection CreateReadOnlyConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SqlConnection CreateConnection()
        => new(_configuration.GetConnectionString("DefaultConnection"));

    public SqlConnection CreateAdminConnection()
        => new(_configuration.GetConnectionString("AdminConnection"));

    public SqlConnection CreateReadOnlyConnection()
        => new(_configuration.GetConnectionString("ReadOnlyConnection"));
}

# Polaczenia z serwerem - Konfiguracja

## 1. Connection Strings

### Development (lokalny Docker)
```
Server=localhost,1433;Database=CasinoDB;User Id=sa;Password=Haslo123!;Encrypt=False;TrustServerCertificate=True;
```

### Development (konto dev)
```
Server=localhost,1433;Database=CasinoDB;User Id=CasinoDev;Password=CasinoDev_D3v123!;Encrypt=False;TrustServerCertificate=True;
```

### Aplikacja webowa (konto app)
```
Server=localhost,1433;Database=CasinoDB;User Id=CasinoAppUser;Password=CasinoApp_P@ss123!;Encrypt=False;TrustServerCertificate=True;
```

### ReadOnly (raporty/analityka)
```
Server=localhost,1433;Database=CasinoDB;User Id=CasinoReadOnly;Password=CasinoRead_R3p0rt!;Encrypt=False;TrustServerCertificate=True;
```

### Admin (zarzadzanie baza)
```
Server=localhost,1433;Database=CasinoDB;User Id=CasinoAdmin;Password=CasinoAdmin_Str0ng!;Encrypt=False;TrustServerCertificate=True;
```

## 2. Konfiguracja w appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=CasinoDB;User Id=CasinoAppUser;Password=CasinoApp_P@ss123!;Encrypt=False;TrustServerCertificate=True;",
    "AdminConnection": "Server=localhost,1433;Database=CasinoDB;User Id=CasinoAdmin;Password=CasinoAdmin_Str0ng!;Encrypt=False;TrustServerCertificate=True;",
    "ReadOnlyConnection": "Server=localhost,1433;Database=CasinoDB;User Id=CasinoReadOnly;Password=CasinoRead_R3p0rt!;Encrypt=False;TrustServerCertificate=True;",
    "DevConnection": "Server=localhost,1433;Database=CasinoDB;User Id=CasinoDev;Password=CasinoDev_D3v123!;Encrypt=False;TrustServerCertificate=True;"
  }
}
```

## 3. Ustawienia w Program.cs

```csharp
// Polaczenie domyslne (aplikacja)
builder.Services.AddDbContext<Automaty>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()
    ));

// Polaczenie admin (do procedur administracyjnych)
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AdminConnection")
    ));
```

## 4. Docker Compose (lokalna baza)

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=Haslo123!
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql

volumes:
  sqldata:
```

## 5. Role polaczen

| Konto | Uzywane przez | Uprawnienia | Connection Pool |
|-------|--------------|-------------|-----------------|
| CasinoAppUser | Aplikacja webowa | CRUD + procedury | Tak (domyslnie Max 100) |
| CasinoAdmin | Administracja baza | DDL + security | Nie (jednorazowe) |
| CasinoReadOnly | Raporty, analityka | SELECT tylko | Tak |
| CasinoDev | Developerzy | Pelna kontrola (dev) | Nie |

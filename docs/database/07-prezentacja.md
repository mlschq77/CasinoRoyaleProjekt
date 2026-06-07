# Prezentacja - Casino Royale Database Project

## Plan działania (zrealizowany)

### Faza 1: Analiza biznesowa ✅
- Dokument wymagań funkcjonalnych i niefunkcjonalnych
- Lista encji (22 tabele)
- Opis procesów biznesowych

### Faza 2: Skrypt tworzący bazę danych ✅
- `sql/01-create-database.sql` - Baza, loginy, użytkownicy, role, schematy
- `sql/02-create-tables.sql` - 22 tabele z kluczami obcymi
- `sql/03-create-indexes.sql` - 25 indeksów wydajnościowych
- `sql/04-deploy-all.sql` - Skrypt deploymentu all-in-one

### Faza 3: PL/SQL (T-SQL) ✅
- **Procedury** (4): PlaceBet, SettleBet, ApplyBonusCode, GetUserReport
- **Funkcje** (3): GetUserBalance, GetUserGameStats, GetActiveBonuses
- **Triggery** (3): BetRecord audit, LoginHistory audit, Wallet bonus expiry

### Faza 4: ORM - Entity Framework Core ✅
- DbContext `Automaty.cs` z 22 DbSet
- Migracje EF Core (35 migracji)
- Indeksy przez Fluent API
- Connection retry policy

### Faza 5: Endpointy API ✅
- `ApiController.cs` - REST API: bet-history, game-stats, balance
- Paginacja w AdminController (Uzytkownicy, LoginHistory)
- Paginacja w ProfileController (BetHistory)
- Keyset i OFFSET pagination

### Faza 6: Duże dane (Faker/Bogus) ✅
- `FakeDataSeeder.cs` - Generowanie danych testowych
- Parametry: `--seed-users`, `--seed-games`, `--seed-fake-data`
- Konfiguracja liczby użytkowników: `--fake-users=10000`

### Faza 7: Indeksy ✅
- 25 indeksów (w tym 6 filtrowanych/złożonych)
- Migracja EF Core `DatabaseOptimization`
- INCLUDE columns dla wydajności

### Faza 8: EXPLAIN PLAN ✅
- `sql/optimization/01-explain-plan-analysis.sql`
- `sql/optimization/02-query-refactoring.sql`
- `sql/optimization/03-pagination-benchmark.sql`
- Analiza przed/po optymalizacji

### Faza 9: Bezpieczeństwo ✅
- 4 konta bazodanowe z różnymi uprawnieniami
- Cookie Authentication z BCrypt
- Dynamic Data Masking (Email, HasloHash)
- Row-Level Security (RLS przygotowane)
- Anti-forgery, HTTPS, audit log

### Faza 10: Diagramy ✅
- ERD (PlantUML) w `diagrams/erd-casino-royale.puml`
- Diagram klas UML w `docs/diagramy-uml.md`

---

## Struktura projektu (po zmianach)

```
CasinoRoyale/
├── Controllers/
│   ├── AdminController.cs       # + paginacja
│   ├── ProfileController.cs     # + paginacja
│   └── ApiController.cs         # NOWY - REST API
├── Services/
│   └── PaginationHelper.cs      # NOWY - helper paginacji
├── Data/
│   ├── Automaty.cs              # + indeksy
│   ├── DbConnectionFactory.cs   # NOWY - fabryka połączeń
│   └── Migrations/
│       └── ...DatabaseOptimization.cs  # NOWA migracja
├── sql/
│   ├── 01-create-database.sql   # Konta, role, schematy
│   ├── 02-create-tables.sql     # 22 tabele
│   ├── 03-create-indexes.sql    # 25 indeksów
│   ├── 04-deploy-all.sql        # Deployment
│   ├── procedures/              # 4 procedury
│   ├── functions/               # 3 funkcje
│   ├── triggers/                # 3 triggery
│   ├── security/                # Audit, RLS, Masking
│   ├── seed/                    # Kody bonusowe
│   └── optimization/            # Explain Plan, refactoring
├── docs/
│   └── database/
│       ├── 01-analiza-biznesowa.md
│       ├── 02-projekt-bd.md
│       ├── 03-polaczenia-serwer.md
│       ├── 04-optymalizacja.md
│       ├── 05-bezpieczenstwo.md
│       ├── 06-dobre-praktyki.md
│       └── 07-prezentacja.md
└── diagrams/
    └── erd-casino-royale.puml
```

## Komendy uruchomieniowe

```bash
# Deployment bazy danych (SQL Server)
sqlcmd -S localhost,1433 -U sa -P Haslo123! -i sql/04-deploy-all.sql

# Seed danych testowych (10000 uzytkownikow)
dotnet run --seed-fake-data --fake-users=10000

# Tylko seed uzytkownikow
dotnet run --seed-users --fake-users=500

# Tylko seed katalogu gier
dotnet run --seed-games

# Dodanie migracji EF Core
dotnet ef migrations add NazwaMigracji

# Aplikacja migracji
dotnet ef database update
```

## Connection Strings

| Środowisko | Connection String |
|------------|------------------|
| Docker (sa) | `Server=localhost,1433;Database=CasinoDB;User Id=sa;Password=Haslo123!;Encrypt=False;TrustServerCertificate=True;` |
| App User | `Server=localhost,1433;Database=CasinoDB;User Id=CasinoAppUser;Password=CasinoApp_P@ss123!;Encrypt=False;TrustServerCertificate=True;` |
| Admin | `Server=localhost,1433;Database=CasinoDB;User Id=CasinoAdmin;Password=CasinoAdmin_Str0ng!;Encrypt=False;TrustServerCertificate=True;` |
| ReadOnly | `Server=localhost,1433;Database=CasinoDB;User Id=CasinoReadOnly;Password=CasinoRead_R3p0rt!;Encrypt=False;TrustServerCertificate=True;` |

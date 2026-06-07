# CasinoRoyale

CasinoRoyale to aplikacja webowa ASP.NET Core MVC symulująca platformę kasynową. Projekt zawiera katalog gier, konta użytkowników, portfel z podziałem na środki realne i bonusowe, płatności testowe Stripe, weryfikację KYC, panel administracyjny oraz historię rozgrywek.

> Projekt ma charakter edukacyjny/demonstracyjny. Nie jest gotowym systemem produkcyjnym do obsługi realnych gier hazardowych.

## Najważniejsze funkcje

- Rejestracja, logowanie i wylogowanie użytkowników z użyciem uwierzytelniania cookie.
- Hashowanie haseł przez `BCrypt.Net-Next`.
- Katalog gier oraz widoki gier dostępne po zalogowaniu.
- Gry: Mines, Plinko, Fruits, Blackjack, Crash, Roulette, Dice, Keno i Baccarat.
- Portfel użytkownika z saldem realnym, saldem bonusowym oraz wymaganym obrotem bonusu.
- Obsługa wpłat i wypłat testowych przez Stripe.
- Kody bonusowe z minimalną wpłatą, bonusem procentowym/kwotowym, datą ważności i wageringiem.
- Weryfikacja KYC z przesyłaniem dokumentów i decyzją administratora.
- Panel administratora z użytkownikami, saldami, uprawnieniami, kodami bonusowymi, KYC, historią logowań i statystykami gier.
- Automatyczne wykonywanie migracji bazy danych przy starcie aplikacji.

## Technologie

- .NET 8
- ASP.NET Core MVC
- Entity Framework Core 8
- Microsoft SQL Server 2022
- Bootstrap, jQuery
- Stripe.net
- QuestPDF
- Docker i Docker Compose

## Struktura projektu

```text
CasinoRoyale/
├── Controllers/      Kontrolery MVC i endpointy AJAX/API dla gier
├── Data/             DbContext EF Core oraz dane startowe
├── Models/           Encje domenowe i modele bazodanowe
├── Services/         Logika gier, salda, bonusów, KYC i RNG
├── ViewModels/       Modele widoków
├── Views/            Widoki Razor
├── wwwroot/          Pliki statyczne: CSS, JS, biblioteki frontendowe
├── Migrations/       Migracje Entity Framework Core
├── Dockerfile        Obraz aplikacji ASP.NET Core
└── docker-compose.yml
```

## Wymagania

Do uruchomienia lokalnego bez Dockera:

- .NET SDK 8.0
- SQL Server lub SQL Server Express
- Narzędzie EF Core CLI, jeżeli migracje mają być wykonywane ręcznie:

```powershell
dotnet tool install --global dotnet-ef
```

Do uruchomienia przez Docker:

- Docker Desktop
- Docker Compose

## Konfiguracja

Podstawowa konfiguracja znajduje się w:

- `appsettings.json`
- zmiennych środowiskowych w `docker-compose.yml`

W środowisku produkcyjnym klucze Stripe, hasła do bazy i klucze API przekazywane są przez sekrety lub zmienne środowiskowe, a nie przechowywane w repozytorium.

## Uruchomienie przez Docker

Najprostszy sposób uruchomienia projektu:

```powershell
docker compose up --build
```

Po starcie aplikacja jest dostępna pod adresem:

```text
http://localhost:8080
```

Compose uruchamia dwa serwisy:

- `app` - aplikacja ASP.NET Core na porcie `8080`.
- `db` - SQL Server 2022 na porcie `1433`.

Wolumeny:

- `sqlserver-data` - dane SQL Server.
- `kyc-data` - przesłane pliki KYC w kontenerze aplikacji.

## Uruchomienie lokalne bez Dockera

1. Ustaw connection string w `appsettings.json` albo przez user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=CasinoDB;User Id=sa;Password=Haslo123!;TrustServerCertificate=True;Encrypt=False;"
```

2. Opcjonalnie ustaw klucze Stripe:

```powershell
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_..."
```

3. Uruchom aplikację:

```powershell
dotnet run
```

Migracje EF Core są wykonywane automatycznie przy starcie aplikacji przez `Program.ApplyDatabaseMigrations`.

## Baza danych

Projekt używa `Automaty` jako głównego `DbContext`. Najważniejsze tabele/encje:

- `Users` - użytkownicy, dane profilu, status KYC i flaga administratora.
- `Wallets` - saldo realne, saldo bonusowe i postęp obrotu.
- `BetRecords` - historia zakładów i rozliczeń.
- `StripePayments` oraz `StripeWithdrawals` - płatności i wypłaty Stripe.
- `KodyBonusowe` oraz `UzyteKodyBonusowe` - definicje i użycia bonusów.
- `KycDocuments` - dokumenty KYC.
- Tabele gier, m.in. `MinesGames`, `PlinkoGames`, `BlackjackGames`, `CrashSessions`, `RouletteGames`, `DiceGames`, `KenoGames`, `BaccaratGames`.

Ręczne wykonanie migracji:

```powershell
dotnet ef database update
```

Dodanie nowej migracji:

```powershell
dotnet ef migrations add NazwaMigracji
```

## Główne moduły

### Użytkownicy i autoryzacja

Za logowanie i rejestrację odpowiada `AuthController`. Sesja użytkownika jest oparta o cookie `CasinoRoyale.Auth`. W claims przechowywane są m.in. identyfikator użytkownika, nazwa, email, informacja o uprawnieniach administratora i aktualny balans.

### Gry

Widoki gier obsługuje `GamesController`, a akcje rozgrywki są rozdzielone na dedykowane kontrolery:

- `MinesController`
- `PlinkoController`
- `FruitsController`
- `BlackjackController`
- `CrashController`
- `RouletteController`
- `DiceController`
- `KenoController`
- `BaccaratController`

Logika domenowa gier znajduje się w odpowiadających im serwisach w katalogu `Services`.

### Portfel i bonusy

`BalanceService` odpowiada za stawianie zakładów, wypłaty wygranych i rozliczanie środków. `BonusCodeService` waliduje i nalicza bonusy powiązane z wpłatami. Bonusy są trzymane oddzielnie od salda realnego, a wypłata jest blokowana do momentu spełnienia wymagań obrotu.

### Płatności

`PaymentsController` obsługuje:

- formularz wpłaty,
- walidację kodu bonusowego,
- tworzenie sesji Stripe Checkout,
- potwierdzenie płatności,
- anulowanie płatności,
- zlecenie wypłaty Stripe Connect.

Wpłaty i wypłaty są dostępne tylko dla użytkowników z zatwierdzonym KYC.

### KYC

`KycController` pozwala użytkownikowi przesłać dokument. `KycService` zapisuje pliki i obsługuje statusy dokumentów. Administrator może zatwierdzać i odrzucać dokumenty w `AdminController`.

### Panel administratora

Panel administratora jest dostępny dla zalogowanych użytkowników z `IsAdmin = true`. Funkcje:

- dashboard z podsumowaniem użytkowników, sald, wpłat i wypłat,
- lista użytkowników i edycja salda,
- nadawanie/odbieranie uprawnień administratora,
- usuwanie użytkowników bez uprawnień admina,
- zarządzanie kodami bonusowymi,
- obsługa dokumentów KYC,
- historia logowań,
- statystyki gier.

## Najważniejsze ścieżki

| Ścieżka | Opis |
| --- | --- |
| `/` | Strona główna |
| `/Auth/Rejestracja` | Rejestracja |
| `/Auth/Logowanie` | Logowanie |
| `/Automaty/Oferta` | Katalog gier |
| `/Games/Mines` | Mines |
| `/Games/Plinko` | Plinko |
| `/Games/Fruits` | Fruits |
| `/Games/Blackjack` | Blackjack |
| `/Games/Crash` | Crash |
| `/Games/Roulette` | Roulette |
| `/Games/Dice` | Dice |
| `/Games/Keno` | Keno |
| `/Games/Baccarat` | Baccarat |
| `/Payments/Deposit` | Wpłata |
| `/Payments/Withdraw` | Wypłata |
| `/Kyc` | Status KYC |
| `/Kyc/Upload` | Wysyłka dokumentu KYC |
| `/Admin` | Panel administratora |
| `/promocje` | Promocje |
| `/provably-fair` | Informacje o uczciwości gier |
| `/regulamin` | Regulamin |
| `/grajodpowiedzialnie` | Odpowiedzialna gra |

# Casino Royale

Platforma kasynowa zbudowana w ASP.NET Core 8 MVC jako projekt studencki. Umożliwia rejestrację, wpłaty przez Stripe, rozgrywkę w 9 grach kasynowych oraz zarządzanie kontem przez panel administracyjny.

---

## Spis treści

- [Funkcje](#funkcje)
- [Stos technologiczny](#stos-technologiczny)
- [Gry](#gry)
- [Wymagania](#wymagania)
- [Uruchamianie lokalne](#uruchamianie-lokalne)
- [Uruchamianie przez Docker](#uruchamianie-przez-docker)
- [Konfiguracja](#konfiguracja)
- [Testy](#testy)
- [Struktura projektu](#struktura-projektu)
- [Autorzy](#autorzy)

---

## Funkcje

### Użytkownicy
- Rejestracja i logowanie z hashowaniem haseł (BCrypt)
- Sesja oparta na cookies (2h lub 30 dni przy "Zapamiętaj mnie")
- Historia logowań z adresem IP i user-agentem
- KYC (weryfikacja tożsamości): przesyłanie dokumentów, statusy (Oczekujący / Zatwierdzony / Odrzucony)
- Profil użytkownika z historią gier i bonusami

### System salda
- Dwa portfele: saldo realne i saldo bonusowe
- Wymóg obrotu dla środków bonusowych (domyślnie 20x)
- Automatyczne przeliczanie bonusu na środki realne po spełnieniu wymogu

### Kody bonusowe
- Kwotowe i procentowe
- Minimalne wymagania wpłaty
- Data wygaśnięcia i jednorazowe użycie na konto

### Płatności
- Wpłaty przez Stripe Checkout
- Wypłaty przez Stripe Transfers
- Integracja webhooków Stripe

### Panel admina
- Statystyki platformy (użytkownicy, salda, wpłaty/wypłaty)
- Zarządzanie użytkownikami
- Przeglądanie i akceptowanie dokumentów KYC
- Tworzenie i zarządzanie kodami bonusowymi

---

## Stos technologiczny

| Warstwa | Technologia |
|---|---|
| Backend | ASP.NET Core 8.0 MVC |
| Baza danych | SQL Server 2022 + Entity Framework Core 8 |
| Frontend | Razor Views, Bootstrap, jQuery, JavaScript |
| Autoryzacja | Cookie Authentication (własna implementacja) |
| Płatności | Stripe.net 51.1.0 |
| RNG | Random.org API (provably fair) |
| Mapowanie | AutoMapper 16.1.1 |
| Hasła | BCrypt.Net-Next 4.1.0 |
| PDF | QuestPDF |
| Testy | xUnit, Moq, EF Core InMemory |
| Konteneryzacja | Docker, Docker Compose |
| CI/CD | GitHub Actions |

---

## Gry

| Gra | Opis |
|---|---|
| **Mines** | Odkrywanie pól na siatce — unikaj min |
| **Plinko** | Piłka spada przez kołki, trafia do slotów o różnych mnożnikach |
| **Blackjack** | Klasyczny blackjack z krupierem |
| **Roulette** | Koło ruletki z zakładami na liczby, kolory i grupy |
| **Crash** | Mnożnik rośnie — wypłać się zanim wykres się rozbije |
| **Dice** | Przewidywanie wyniku rzutu kostką |
| **Keno** | Wybieranie liczb w stylu loterii |
| **Baccarat** | Karta przeciwko bankierowi |
| **Automaty** | Katalog automatów od zewnętrznych dostawców |

---

## Wymagania

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (lokalny lub przez Docker)
- Klucze Stripe (testowe dostępne bezpłatnie na [stripe.com](https://stripe.com))
- Opcjonalnie: klucz API Random.org

---

## Uruchamianie lokalne

```bash
# 1. Klonuj repozytorium
git clone https://github.com/<twoj-user>/CasinoRoyaleProjekt.git
cd CasinoRoyaleProjekt

# 2. Ustaw connection string i klucze w appsettings.Development.json

# 3. Zastosuj migracje bazy danych
dotnet ef database update

# 4. Uruchom aplikację
dotnet run

# Aplikacja dostępna pod: http://localhost:5018
```

---

## Uruchamianie przez Docker

```bash
# Uruchom całe środowisko (aplikacja + SQL Server)
docker-compose up --build
```

Aplikacja będzie dostępna pod `http://localhost:8080`.

Docker Compose automatycznie:
- uruchamia SQL Server 2022,
- czeka na gotowość bazy,
- aplikuje migracje przy starcie kontenera.

**Wolumeny:**
- `sqlserver-data` — dane bazy danych
- `kyc-data` — przesłane dokumenty KYC

---

## Konfiguracja

Klucze konfiguracyjne w `appsettings.json` (lub zmienne środowiskowe w Docker):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<connection-string-do-sql-server>"
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  },
  "RandomOrgOptions": {
    "ApiEndpoint": "https://api.random.org/json-rpc/4/invoke"
  }
}
```

> Nie commituj kluczy produkcyjnych. Używaj `dotnet user-secrets` lub zmiennych środowiskowych.

---

## Testy

Projekt zawiera testy jednostkowe w `CasinoRoyale.Tests` (xUnit + Moq):

```bash
dotnet test
```

Pokrycie testami:
- Logika gier: Mines, Plinko, Fruits, Blackjack, Crash, Dice, Keno, Baccarat, Roulette
- Serwisy: Balance, BonusCode
- Kontrolery: AuthController

Testy używają EF Core InMemory — nie wymagają zewnętrznej bazy danych.

---

## Struktura projektu

```
CasinoRoyaleProjekt/
├── Controllers/          # Kontrolery HTTP (gry, auth, admin, płatności, KYC)
├── Models/               # Modele EF Core (User, Wallet, gry, płatności, KYC)
├── Services/             # Logika biznesowa (gry, saldo, bonusy, KYC, RNG)
├── Data/                 # DbContext (Automaty.cs) + fabryka
├── Views/                # Widoki Razor
│   ├── Auth/             # Logowanie, rejestracja
│   ├── Games/            # UI gier
│   ├── Admin/            # Panel administracyjny
│   ├── Payments/         # Wpłaty, wypłaty
│   ├── Kyc/              # Weryfikacja dokumentów
│   ├── Profile/          # Profil użytkownika
│   └── Shared/           # Layout, partiale
├── ViewModels/           # Modele formularzy
├── ViewComponents/       # BalanceViewComponent
├── Migrations/           # Migracje EF Core
├── CasinoRoyale.Tests/   # Projekt testowy
├── docs/                 # Diagramy UML
├── Dockerfile
├── docker-compose.yml
└── Program.cs
```

---

## Autorzy

Projekt studencki — rozwijany w ramach kursu programowania .NET.

---

## Licencja

Projekt edukacyjny — wyłącznie do celów niekomercyjnych.

# Casino Royale

A casino platform built with ASP.NET Core 8 MVC as a university project. It supports user registration, deposits via Stripe, nine casino games, and account management through an admin panel.

## Table of contents

- [Features](#features)
- [Tech stack](#tech-stack)
- [Games](#games)
- [Requirements](#requirements)
- [Running locally](#running-locally)
- [Running with Docker](#running-with-docker)
- [Configuration](#configuration)
- [Tests](#tests)
- [Project structure](#project-structure)
- [Authors](#authors)
- [License](#license)

## Features

### Users
- Registration and login with password hashing (BCrypt)
- Cookie-based sessions (2 hours, or 30 days with "Remember me")
- Login history including IP address and user agent
- KYC (identity verification): document upload with Pending / Approved / Rejected statuses
- User profile with game history and bonuses

### Balance system
- Two wallets: real balance and bonus balance
- Wagering requirement on bonus funds (20x by default)
- Automatic conversion of bonus funds to real funds once the requirement is met

### Bonus codes
- Fixed-amount and percentage-based
- Minimum deposit requirements
- Expiry date and single use per account

### Payments
- Deposits via Stripe Checkout
- Withdrawals via Stripe Transfers
- Stripe webhook integration

### Admin panel
- Platform statistics (users, balances, deposits and withdrawals)
- User management
- Review and approval of KYC documents
- Creation and management of bonus codes

## Tech stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8.0 MVC |
| Database | SQL Server 2022 + Entity Framework Core 8 |
| Frontend | Razor Views, Bootstrap, jQuery, JavaScript |
| Authentication | Cookie authentication (custom implementation) |
| Payments | Stripe.net 51.1.0 |
| RNG | Random.org API (provably fair) |
| Mapping | AutoMapper 16.1.1 |
| Passwords | BCrypt.Net-Next 4.1.0 |
| PDF | QuestPDF |
| Tests | xUnit, Moq, EF Core InMemory |
| Containerisation | Docker, Docker Compose |
| CI/CD | GitHub Actions |

## Games

| Game | Description |
|---|---|
| Mines | Reveal tiles on a grid while avoiding mines |
| Plinko | A ball drops through pegs into slots with different multipliers |
| Blackjack | Classic blackjack against the dealer |
| Roulette | Roulette wheel with bets on numbers, colours and groups |
| Crash | The multiplier climbs — cash out before the graph crashes |
| Dice | Predict the outcome of a dice roll |
| Keno | Lottery-style number selection |
| Baccarat | Player card against the banker |
| Slots | Catalogue of slot machines from third-party providers |

## Requirements

- .NET 8 SDK
- SQL Server (local or via Docker)
- Stripe API keys (test keys are available free at stripe.com)
- Optional: Random.org API key

## Running locally

```bash
# 1. Clone the repository
git clone https://github.com/mlschq77/CasinoRoyaleProjekt.git
cd CasinoRoyaleProjekt

# 2. Set the connection string and API keys in appsettings.Development.json

# 3. Apply database migrations
dotnet ef database update

# 4. Run the application
dotnet run

# The app is available at http://localhost:5018
```

## Running with Docker

```bash
# Start the full environment (application + SQL Server)
docker-compose up --build
```

The app will be available at http://localhost:8080.

Docker Compose automatically:
- starts SQL Server 2022,
- waits for the database to become ready,
- applies migrations on container startup.

Volumes:
- `sqlserver-data` — database data
- `kyc-data` — uploaded KYC documents

## Configuration

Configuration keys live in `appsettings.json` (or as environment variables when running in Docker):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<sql-server-connection-string>"
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

> Never commit production keys. Use `dotnet user-secrets` or environment variables.

## Tests

The project includes unit tests in `CasinoRoyale.Tests` (xUnit + Moq):

```bash
dotnet test
```

Test coverage:
- Game logic: Mines, Plinko, Fruits, Blackjack, Crash, Dice, Keno, Baccarat, Roulette
- Services: Balance, BonusCode
- Controllers: AuthController

Tests run on EF Core InMemory and require no external database.

## Project structure

```
CasinoRoyaleProjekt/
├── Controllers/          # HTTP controllers (games, auth, admin, payments, KYC)
├── Models/               # EF Core models (User, Wallet, games, payments, KYC)
├── Services/             # Business logic (games, balance, bonuses, KYC, RNG)
├── Data/                 # DbContext (Automaty.cs) + factory
├── Views/                # Razor views
│   ├── Auth/             # Login, registration
│   ├── Games/            # Game UI
│   ├── Admin/            # Admin panel
│   ├── Payments/         # Deposits, withdrawals
│   ├── Kyc/              # Document verification
│   ├── Profile/          # User profile
│   └── Shared/           # Layout, partials
├── ViewModels/           # Form models
├── ViewComponents/       # BalanceViewComponent
├── Migrations/           # EF Core migrations
├── CasinoRoyale.Tests/   # Test project
├── docs/                 # UML diagrams
├── Dockerfile
├── docker-compose.yml
└── Program.cs
```

## Authors

University project developed as part of a .NET programming course.

- Tomasz Chodorowski
- Miłosz Michalski
- Maciej Klepacki
- Gabriel Smyk

## License

Educational project — for non-commercial use only.

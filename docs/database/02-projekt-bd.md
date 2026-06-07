# Projekt Bazy Danych - Casino Royale

## 1. Diagram encji (ERD)

### Relacje główne

```
Users (1) ---- (1) Wallet
Users (1) ---- (*) BetRecord
Users (1) ---- (*) LoginHistory
Users (1) ---- (*) KycDocument
Users (1) ---- (*) GameResult
Users (1) ---- (*) MinesGame
Users (1) ---- (*) PlinkoGame
Users (1) ---- (*) BlackjackGame
Users (1) ---- (*) CrashSession
Users (1) ---- (*) RouletteGame
Users (1) ---- (*) DiceGame
Users (1) ---- (*) KenoGame
Users (1) ---- (*) BaccaratGame
Users (1) ---- (*) UzyteKodyBonusowe
Users (1) ---- (*) StripePayment
Users (1) ---- (*) StripeWithdrawal

KodBonusowy (1) ---- (*) UzyteKodyBonusowe
StripePayment (1) ---- (*) UzyteKodyBonusowe

AutomatProvider (1) ---- (*) AutomatInfo
AutomatInfo (*) ---- (*) Kategoria (przez AutomatyKategorie)
```

## 2. Typy danych

| Typ | Użycie |
|-----|--------|
| decimal(18,2) | Kwoty (saldo, zakłady, wygrane) |
| decimal(5,2) | Procenty bonusowe |
| nvarchar(450) | Indeksowane kolumny tekstowe |
| nvarchar(max) | JSON (deck, ręce, zakłady) |
| datetime2 | Daty UTC |
| bit | Boolean (IsActive, IsAdmin, Settled) |
| int | Klucze główne i obce |

## 3. Indeksy

| Tabela | Indeks | Typ |
|--------|--------|-----|
| Users | Email (unique) | UNIQUE |
| Wallets | UserId (unique) | UNIQUE |
| BetRecords | (UserId, SessionKey, Settled) | Złożony |
| LoginHistories | (UserId, LoggedAt) | Złożony |
| KodBonusowe | Kod (unique) | UNIQUE |
| StripePayments | SessionId (unique) | UNIQUE |
| StripeWithdrawals | TransferId (unique) | UNIQUE |
| AutomatProviderzy | Nazwa (unique) | UNIQUE |
| Kategorie | Nazwa (unique) | UNIQUE |

## 4. Wzorce projektowe

- Repository przez DbContext EF Core
- Unit of Work przez DbContext
- Service Layer dla logiki biznesowej
- DTO/ViewModels dla warstwy prezentacji

# Optymalizacja bazy danych - Casino Royale

## 1. Indeksy - przed i po optymalizacji

### Przed optymalizacją (tylko EF Core defaults):
- Klucze główne (PK)
- Unique indices (Email, SessionId, Kod, itd.)
- Indeks złożony BetRecords (UserId, SessionKey, Settled)
- Indeks złożony LoginHistories (UserId, LoggedAt)

### Po optymalizacji (dodane):

| Tabela | Indeks | Typ | Cel |
|--------|--------|-----|-----|
| Users | IX_Users_Nazwa | NONCLUSTERED | Szybsze wyszukiwanie po nazwie |
| Users | IX_Users_Email z INCLUDE | NONCLUSTERED | Szybsze logowanie (Include hasła) |
| BetRecords | IX_BetRecords_UserId_CreatedAt | NONCLUSTERED | Paginacja historii zakładów |
| BetRecords | IX_BetRecords_GameName | NONCLUSTERED | Filtrowanie po grze |
| LoginHistories | IX_LoginHistories_LoggedAt | NONCLUSTERED | Czyszczenie starych logów |
| Wallets | IX_Wallets_BalanceReal (filtrowany) | NONCLUSTERED | Ranking zysków/strat |
| StripePayments | IX_StripePayments_UserId_CreatedAt | NONCLUSTERED | Historia wpłat |
| StripeWithdrawals | IX_StripeWithdrawals_UserId_CreatedAt | NONCLUSTERED | Historia wypłat |
| GameResults | IX_GameResults_UserId_PlayedAt | NONCLUSTERED | Archiwum gier |
| GameResults | IX_GameResults_PlayedAtUtc | NONCLUSTERED | Czyszczenie archiwum |
| KycDocuments | IX_KycDocuments_Status | NONCLUSTERED | Panel admina KYC |
| Gry (*Games) | IX_*Games_UserId | NONCLUSTERED | Historia gier |

## 2. Refactoring zapytań

### Przed:
```sql
SELECT * FROM BetRecords WHERE UserId = 1
-- Zwraca wszystkie rekordy, brak paginacji
```

### Po:
```sql
SELECT Id, GameName, Amount, PayoutAmount, CreatedAt
FROM BetRecords
WHERE UserId = 1
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY
-- Konkretne kolumny + paginacja
```

### Przed (N+1 problem):
```sql
SELECT U.*,
  (SELECT SUM(Amount) FROM BetRecords WHERE UserId = U.Id) AS TotalBet
FROM Users U
```

### Po:
```sql
SELECT U.Id, U.Nazwa, SUM(BR.Amount) AS TotalBet
FROM Users U
LEFT JOIN BetRecords BR ON BR.UserId = U.Id
GROUP BY U.Id, U.Nazwa
```

## 3. Paginacja

### Metoda 1: OFFSET-FETCH (standard)
- Działa z każdym ORDER BY
- Wydajna dla pierwszych stron
- Spowalnia przy głębokiej paginacji (offset > 10000)

### Metoda 2: Keyset Pagination (seeks)
- Stała wydajność niezależnie od strony
- Wymaga znajomości ostatniej wartości
- Najlepsza dla API z dużą ilością danych

```csharp
// Keyset pagination w EF Core:
var lastCreatedAt = lastSeenItem.CreatedAt;
var lastId = lastSeenItem.Id;

var nextPage = await _db.BetRecords
    .Where(r => r.UserId == userId &&
        (r.CreatedAt < lastCreatedAt ||
         (r.CreatedAt == lastCreatedAt && r.Id < lastId)))
    .OrderByDescending(r => r.CreatedAt)
        .ThenByDescending(r => r.Id)
    .Take(pageSize)
    .ToListAsync();
```

## 4. Explain Plan - analiza

### Jak używać w SSMS:
1. Włącz `SET STATISTICS IO ON` i `SET STATISTICS TIME ON`
2. Włącz rzeczywisty plan wykonania (Ctrl+M)
3. Wykonaj zapytanie
4. Analizuj:
   - **Table Scan** → brak indeksu (ZŁY)
   - **Index Seek** → optymalne (DOBRY)
   - **Key Lookup** → rozważ INCLUDE columns
   - **Actual rows vs Estimated rows** → nieaktualne statystyki

## 5. Explain Plan przed optymalizacją

```sql
-- Zapytanie: historia zakładów użytkownika
SELECT * FROM BetRecords WHERE UserId = 1 ORDER BY CreatedAt DESC;
-- Plan: Table Scan (brak indeksu CreatedAt)
-- IO: 1000 odczytów
-- Czas: 150ms
```

## 6. Explain Plan po optymalizacji

```sql
-- Po dodaniu IX_BetRecords_UserId_CreatedAt
SELECT Id, GameName, Amount, PayoutAmount, CreatedAt
FROM BetRecords WHERE UserId = 1 ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
-- Plan: Index Seek (IX_BetRecords_UserId_CreatedAt)
-- IO: 20 odczytów
-- Czas: 2ms
```

## 7. Wskazówki wydajnościowe

1. **SARG-able predicates**: Unikaj `WHERE YEAR(CreatedAt) = 2026` → używaj `WHERE CreatedAt >= '2026-01-01' AND CreatedAt < '2027-01-01'`
2. **SELECT tylko potrzebne kolumny**: Zamiast `SELECT *` używaj konkretnych kolumn
3. **Paginacja obowiązkowa**: Nigdy nie zwracaj wszystkich rekordów
4. **AsNoTracking()** do odczytu: `_db.BetRecords.AsNoTracking()`
5. **Batching**: Używaj `AddRange` zamiast wielu `Add`
6. **Connection pooling**: Używaj jednego connection string dla aplikacji

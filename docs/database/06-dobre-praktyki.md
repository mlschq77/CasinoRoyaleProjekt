# Dobre praktyki programowania - Casino Royale

## 1. Architektura warstwowa

```
Controllers (endpointy)
    ↓
Services (logika biznesowa)
    ↓
Data / DbContext (ORM / dostęp do danych)
    ↓
Models (encje)
```

## 2. ORM - Entity Framework Core

### Konfiguracja
- Fluent API w `OnModelCreating` (nie data adnotations dla BD)
- Migracje dla zmian schematu
- Connection retry dla przejściowych błędów

### Dobre praktyki EF Core
```csharp
// DOBRY: AsNoTracking dla odczytu
var users = await _db.Users.AsNoTracking().ToListAsync();

// DOBRY: Konkretne kolumny (SELECT * unikać)
var names = await _db.Users.Select(u => new { u.Id, u.Nazwa }).ToListAsync();

// DOBRY: Paginacja
var page = await _db.BetRecords
    .Where(r => r.UserId == userId)
    .OrderByDescending(r => r.CreatedAt)
    .Skip((pageNum - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

// DOBRY: Batching
_db.Users.AddRange(users);
await _db.SaveChangesAsync();
```

## 3. Service Layer
- Logika biznesowa w serwisach, nie w kontrolerach
- Dependency Injection przez konstruktor
- Interfejsy dla łatwiejszego testowania

## 4. Endpointy API (REST)

| Metoda | Endpoint | Opis |
|--------|----------|------|
| GET | /api/bet-history?page=1&pageSize=20 | Historia zakładów |
| GET | /api/game-stats | Statystyki gier |
| GET | /api/balance | Stan konta |

### Dobre praktyki API:
- Paginacja (page + pageSize)
- Filtrowanie (opcjonalne query params)
- Sortowanie (domyślnie po dacie DESC)
- Status codes (200, 400, 401, 403, 404)
- JSON camelCase

## 5. ViewModels/DTO
- Nie wystawiać encji bezpośrednio (over-posting)
- Używać AutoMapper lub ręcznego mapowania
- Walidacja przez data annotations

## 6. Async/Await
```csharp
// WSZYSTKIE operacje BD asynchroniczne
await _db.SaveChangesAsync();
await _db.Users.ToListAsync();
await _db.Users.FindAsync(id);
```

## 7. Obsługa błędów
- Try-catch w serwisach
- Global exception handler w Program.cs
- Logowanie błędów (ILogger)
- Przyjazne komunikaty dla użytkownika

## 8. Konfiguracja
- appsettings.json → appsettings.Development.json
- UserSecrets dla haseł dev
- Zmienne środowiskowe dla produkcji
- Nigdy nie commitować secretów

## 9. Testowanie
- Testy jednostkowe serwisów
- Testy integracyjne BD
- Seed danych testowych (Bogus/Faker)

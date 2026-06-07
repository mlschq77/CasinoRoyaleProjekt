# Bezpieczeństwo danych - Casino Royale

## 1. Uwierzytelnianie (Authentication)

### Cookie Authentication
- Schemat: `CookieAuthenticationDefaults.AuthenticationScheme`
- Ciasteczko: `CasinoRoyale.Auth` (HttpOnly = true)
- Timeout: 2 godziny (30 dni "Zapamiętaj mnie")
- Logout: Usunięcie ciasteczka

### Hasła
- Algorytm: BCrypt (BCrypt.Net-Next)
- Hashowanie przy rejestracji
- Weryfikacja przy logowaniu
- Nigdy nie przechowujemy plain-text

```csharp
// Hashowanie
user.HasloHash = BCrypt.Net.BCrypt.HashPassword(model.Haslo);

// Weryfikacja
BCrypt.Net.BCrypt.Verify(model.Haslo, user.HasloHash)
```

## 2. Autoryzacja (Authorization)

### Role systemowe

| Rola | Opis | Atrybut |
|------|------|---------|
| User | Standardowy użytkownik | `[Authorize]` |
| Admin | Administrator | `user.IsAdmin == true` |

### Claims
```csharp
new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
new Claim(ClaimTypes.Name, user.Nazwa)
new Claim("IsAdmin", user.IsAdmin.ToString())
new Claim("Balance", balance.ToString())
```

### Sprawdzanie admina:
```csharp
if (!await IsAdminAsync()) return Forbid();
```

## 3. Role na poziomie bazy danych

| Login DB | Cel | Uprawnienia |
|----------|-----|-------------|
| CasinoAdmin | Administracja BD | db_owner, UNMASK |
| CasinoAppUser | Aplikacja web | db_datareader + db_datawriter |
| CasinoReadOnly | Raporty | db_datareader |
| CasinoDev | Developerzy | db_owner (dev) |

## 4. Dynamic Data Masking
```sql
-- Email: widoczne tylko pierwsze znaki
ALTER TABLE Users ALTER COLUMN Email ADD MASKED WITH (FUNCTION = 'email()');

-- HasloHash: w pełni zasłonięte
ALTER TABLE Users ALTER COLUMN HasloHash ADD MASKED WITH (FUNCTION = 'default()');

-- Admin widzi odmaskowane dane
GRANT UNMASK TO CasinoAdmin;
```

## 5. Dobre praktyki programowania

1. **Never trust input** - Walidacja po stronie serwera (ModelState.IsValid)
2. **Anti-forgery token** - `[ValidateAntiForgeryToken]` na wszystkich POST
3. **SQL Injection** - EF Core parametryzuje zapytania (bezpieczne)
4. **XSS** - Razor automatycznie encoduje output
5. **CSRF** - Anti-forgery token we wszystkich formularzach
6. **HTTPS** - Wymuszone w produkcji (UseHsts)
7. **Connection strings** - W UserSecrets/dev, nie w kodzie źródłowym

## 6. KYC (Know Your Customer)
- Statusy: NotSubmitted → Pending → Approved/Rejected
- Reset KYC przy zmianie danych osobowych
- Tylko admin może zatwierdzać/odrzucać dokumenty

## 7. Logowanie auditowe
- LoginHistories: wszystkie próby logowania
- Audit.FailedLogins: trigger na nieudane logowania
- BetRecords: każdy zakład z sesją
- StripePayments/Withdrawals: pełna historia finansowa

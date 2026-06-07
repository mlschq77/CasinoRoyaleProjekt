# Analiza Biznesowa - Casino Royale

## 1. Opis projektu
Platforma kasyna online z grami originals (Mines, Plinko, Blackjack, Crash, Roulette, Dice, Keno, Baccarat, Fruits) oraz katalogiem automatów od zewnętrznych providerów.

## 2. Wymagania funkcjonalne
- Rejestracja i logowanie użytkowników
- Przeglądanie katalogu gier (automaty + originals)
- Granie w gry z użyciem prawdziwego/ bonusowego salda
- System bonusów (kody bonusowe z wageringiem)
- System płatności (Stripe - depozyty / wypłaty)
- KYC (weryfikacja tożsamości)
- Historia logowań i zakładów
- Panel administratora

## 3. Wymagania niefunkcjonalne
- Bezpieczeństwo: hasła hashowane BCrypt, cookie authentication
- Skalowalność: indeksy, paginacja, optymalizacja zapytań
- Wydajność: Explain Plan, indeksy na kluczowych kolumnach
- Niezawodność: retry na connection string, migracje EF Core

## 4. Encje (tabele)

| Tabela | Opis |
|--------|------|
| Users | Użytkownicy platformy |
| Wallets | Portfele (saldo real + bonus) |
| BetRecords | Historia zakładów |
| KodyBonusowe | Kody promocyjne |
| UzyteKodyBonusowe | Użyte kody przez użytkowników |
| StripePayments | Płatności Stripe |
| StripeWithdrawals | Wypłaty Stripe |
| LoginHistories | Historia logowań |
| KycDocuments | Dokumenty KYC |
| GameResults | Wyniki gier (archiwum) |
| AutomatyInfo | Katalog automatów |
| AutomatProviderzy | Providerzy gier |
| Kategorie | Kategorie gier |
| AutomatyKategorie | Powiązanie automat-kategoria (M:N) |
| MinesGames | Gry Mines |
| PlinkoGames | Gry Plinko |
| BlackjackGames | Gry Blackjack |
| CrashSessions | Gry Crash |
| RouletteGames | Gry Roulette |
| DiceGames | Gry Dice |
| KenoGames | Gry Keno |
| BaccaratGames | Gry Baccarat |

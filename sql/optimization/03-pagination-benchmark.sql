-- ============================================================
-- BENCHMARK PAGINACJI - Porownanie OFFSET vs Keyset
-- ============================================================
USE [CasinoDB];
GO

-- ============================================================
-- METODA 1: OFFSET-FETCH (standardowa paginacja)
-- Zalety: Prosta, dziala z kazdym ORDER BY
-- Wady: Wolniejsza przy glebokiej paginacji (wysoki OFFSET)
-- ============================================================
PRINT '=== METODA 1: OFFSET-FETCH ===';
SET STATISTICS TIME ON;

SELECT
    Id, UserId, GameName, Amount, PayoutAmount, CreatedAt
FROM [app].[BetRecords]
ORDER BY CreatedAt DESC, Id DESC
OFFSET 10000 ROWS FETCH NEXT 20 ROWS ONLY;

SET STATISTICS TIME OFF;
GO

-- ============================================================
-- METODA 2: Keyset Pagination (seeks)
-- Zalety: Szybka, staly czas niezaleznie od strony
-- Wady: Wymaga unikalnego sortowania, zna ostatnia wartosc
-- ============================================================
PRINT '=== METODA 2: Keyset Pagination ===';
SET STATISTICS TIME ON;

DECLARE @LastCreatedAt DATETIME2 = '2026-06-01 00:00:00';
DECLARE @LastId INT = 999999;

SELECT TOP 20
    Id, UserId, GameName, Amount, PayoutAmount, CreatedAt
FROM [app].[BetRecords]
WHERE CreatedAt < @LastCreatedAt
   OR (CreatedAt = @LastCreatedAt AND Id < @LastId)
ORDER BY CreatedAt DESC, Id DESC;

SET STATISTICS TIME OFF;
GO

-- ============================================================
-- METODA 3: Stronicowanie przez kursor (niezalecane)
-- ============================================================
PRINT '=== METODA 3: Kursor (odradzane) ===';
SET STATISTICS TIME ON;

DECLARE @CursorBetRecords CURSOR;
-- To tylko przyklad - w praktyce unikac kursorow dla paginacji

SET STATISTICS TIME OFF;
GO

PRINT '=== WNIOSKI ===';
PRINT '1. OFFSET-FETCH: Dobre dla pierwszych stron, pogarsza sie z offsetem';
PRINT '2. Keyset: Stala wydajnosc, najlepszy dla duzych zbiorow';
PRINT '3. Kursor: Unikac - blokuje zasoby, wolny';
PRINT '';
PRINT 'REKOMENDACJA: Uzywac Keyset Pagination dla API z duza iloscia danych';
PRINT 'Uzywac OFFSET-FETCH dla stron admina (pierwsze strony)';
GO

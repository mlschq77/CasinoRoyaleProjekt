-- ============================================================
-- ANALIZA PLANOW ZAPYTAN (EXPLAIN PLAN) - Casino Royale
-- ============================================================
USE [CasinoDB];
GO

-- ============================================================
-- Przykladowe zapytanie 1: Historia zakladow uzytkownika
-- PRZED optymalizacja (bez indeksu)
-- ============================================================
PRINT '=== ANALIZA 1: Historia zakladow uzytkownika ===';
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Wlacz rzeczywisty plan wykonania (w SSMS: Ctrl+M)
-- To zapytanie uzywa IX_BetRecords_UserId_CreatedAt
SELECT
    BR.Id,
    BR.GameName,
    BR.Amount,
    BR.PayoutAmount,
    BR.CreatedAt
FROM [app].[BetRecords] BR
WHERE BR.UserId = 1
ORDER BY BR.CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- Przykladowe zapytanie 2: Suma wygranych per gra
-- ============================================================
PRINT '=== ANALIZA 2: Suma wygranych per gra ===';
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT
    BR.GameName,
    COUNT(*) AS TotalBets,
    ISNULL(SUM(BR.Amount), 0) AS TotalBetAmount,
    ISNULL(SUM(BR.PayoutAmount), 0) AS TotalWinAmount,
    CAST(ISNULL(SUM(BR.PayoutAmount), 0) * 100.0 / NULLIF(SUM(BR.Amount), 0) AS DECIMAL(10,2)) AS RTP
FROM [app].[BetRecords] BR
WHERE BR.Settled = 1
  AND BR.CreatedAt >= DATEADD(DAY, -30, SYSUTCDATETIME())
GROUP BY BR.GameName
ORDER BY TotalBetAmount DESC;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- Przykladowe zapytanie 3: Raport admina - uzytkownicy z najwiekszymi stratami
-- ============================================================
PRINT '=== ANALIZA 3: Top 10 uzytkownikow z najwiekszymi stratami ===';
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT TOP 10
    U.Nazwa,
    U.Email,
    W.BalanceReal,
    (SELECT ISNULL(SUM(BR.Amount), 0) FROM [app].[BetRecords] BR WHERE BR.UserId = U.Id AND BR.Settled = 1) AS TotalBet,
    (SELECT ISNULL(SUM(BR.PayoutAmount), 0) FROM [app].[BetRecords] BR WHERE BR.UserId = U.Id AND BR.Settled = 1) AS TotalWin,
    (SELECT ISNULL(SUM(BR.Amount), 0) FROM [app].[BetRecords] BR WHERE BR.UserId = U.Id AND BR.Settled = 1)
    - (SELECT ISNULL(SUM(BR.PayoutAmount), 0) FROM [app].[BetRecords] BR WHERE BR.UserId = U.Id AND BR.Settled = 1) AS NetLoss
FROM [app].[Users] U
INNER JOIN [app].[Wallets] W ON W.UserId = U.Id
ORDER BY NetLoss DESC;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- Przykladowe zapytanie 4: Sprawdzenie stanu bonusow
-- ============================================================
PRINT '=== ANALIZA 4: Aktywne bonusy ===';
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT * FROM [fn].[GetActiveBonuses]()
ORDER BY ProgressPercent ASC;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
GO

-- ============================================================
-- ANALIZA PLANU: Jak odczytac explain plan
-- ============================================================
PRINT '=== JAK CZYTAC EXPLAIN PLAN ===';
PRINT '1. Table Scan - brak indeksu, pelne skanowanie tabeli (ZLY)';
PRINT '2. Clustered Index Scan - skanowanie klastrowego indeksu';
PRINT '3. Index Seek - szybkie wyszukiwanie przez indeks (DOBRY)';
PRINT '4. Key Lookup - dodatkowe odczyty przez klucz glowny';
PRINT '5. Nested Loops - zlaczenie petla (dobre dla malych zbiorow)';
PRINT '6. Hash Match - zlaczenie przez hash (dobre dla duzych zbiorow)';
PRINT '7. Merge Join - zlaczenie przez merge (dla posortowanych)';
PRINT '';
PRINT 'CELE: Index Seek + Nested Loops, brak Table Scan, niskie IO';
GO

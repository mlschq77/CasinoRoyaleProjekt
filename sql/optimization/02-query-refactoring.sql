-- ============================================================
-- REFACTORING ZAPYTAN - Casino Royale
-- ============================================================
USE [CasinoDB];
GO

-- ============================================================
-- PROBLEM: Subquery w SELECT dla kazdego wiersza (wolne)
-- ============================================================
-- ZLY (N+1 problem):
-- SELECT U.*,
--   (SELECT SUM(Amount) FROM BetRecords WHERE UserId = U.Id) AS TotalBet
-- FROM Users U

-- DOBRY (uzycie GROUP BY lub window functions):
SELECT
    U.Id,
    U.Nazwa,
    U.Email,
    ISNULL(SUM(BR.Amount), 0) AS TotalBet,
    ISNULL(SUM(BR.PayoutAmount), 0) AS TotalWin
FROM [app].[Users] U
LEFT JOIN [app].[BetRecords] BR ON BR.UserId = U.Id AND BR.Settled = 1
GROUP BY U.Id, U.Nazwa, U.Email
ORDER BY TotalBet DESC;
GO

-- ============================================================
-- PROBLEM: Brak paginacji (zwracanie wszystkich rekordow)
-- ============================================================
-- ZLY:
-- SELECT * FROM BetRecords WHERE UserId = 1

-- DOBRY (OFFSET-FETCH):
SELECT
    Id, GameName, Amount, PayoutAmount, CreatedAt
FROM [app].[BetRecords]
WHERE UserId = 1
ORDER BY CreatedAt DESC
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
GO

-- ============================================================
-- PROBLEM: Brak indeksu dla filtrowania po dacie
-- ============================================================
-- ZLY: WHERE YEAR(CreatedAt) = 2026 (funkcja na kolumnie - brak SARG)
-- DOBRY:
SELECT *
FROM [app].[BetRecords]
WHERE CreatedAt >= '2026-01-01' AND CreatedAt < '2027-01-01';
GO

-- ============================================================
-- PROBLEM: SELECT * (zwracanie zbednych kolumn)
-- ============================================================
-- ZLY:
-- SELECT * FROM Users

-- DOBRY:
SELECT Id, Nazwa, Email, DataRejestracji FROM [app].[Users];
GO

-- ============================================================
-- PROBLEM: Brak indeksu dla JOIN + WHERE
-- ============================================================
-- ZAPYTANIE Z REFACTORINGU:
-- Zamiast:
-- SELECT * FROM BetRecords BR
-- INNER JOIN Users U ON U.Id = BR.UserId
-- WHERE U.Email LIKE '%test%'

-- LEPIEJ (najpierw filtruj, potem join):
SELECT BR.*
FROM [app].[BetRecords] BR
WHERE BR.UserId IN (SELECT Id FROM [app].[Users] WHERE Email LIKE '%test%');
GO

PRINT '=== PODSUMOWANIE ZMIAN ===';
PRINT '1. Zastapiono subquery w SELECT na JOIN + GROUP BY';
PRINT '2. Dodano OFFSET-FETCH dla paginacji';
PRINT '3. Uzyto SARG-able predicates (bez funkcji na kolumnach)';
PRINT '4. Zastepowano SELECT * konkretnymi kolumnami';
PRINT '5. Uzyto IN z subquery zamiast JOIN';
GO

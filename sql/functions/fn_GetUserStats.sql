-- ============================================================
-- FUNKCJA TABLE-VALUED: fn.GetUserGameStats
-- Opis: Zwraca statystyki gier dla uzytkownika
-- ============================================================
CREATE OR ALTER FUNCTION [fn].[GetUserGameStats](@UserId INT)
RETURNS TABLE
AS
RETURN
(
    SELECT
        GameName,
        COUNT(*)            AS TotalGames,
        ISNULL(SUM(Amount), 0)   AS TotalBet,
        ISNULL(SUM(PayoutAmount), 0) AS TotalWin,
        CASE WHEN COUNT(*) > 0
            THEN CAST(ISNULL(SUM(PayoutAmount), 0) * 100.0 / NULLIF(SUM(Amount), 0) AS DECIMAL(10,2))
            ELSE 0
        END AS RTPPercent
    FROM [app].[BetRecords]
    WHERE UserId = @UserId
      AND Settled = 1
    GROUP BY GameName
);
GO

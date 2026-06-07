-- ============================================================
-- FUNKCJA TABLE-VALUED: fn.GetActiveBonuses
-- Opis: Zwraca liste aktywnych bonusow
-- ============================================================
CREATE OR ALTER FUNCTION [fn].[GetActiveBonuses]()
RETURNS TABLE
AS
RETURN
(
    SELECT
        W.UserId,
        U.Nazwa AS Username,
        KB.Kod AS BonusCode,
        KB.WageringMultiplier,
        W.WageringRequired,
        W.WageringProgress,
        W.BonusExpiresAt,
        CAST((ISNULL(W.WageringProgress, 0) * 100.0 / NULLIF(W.WageringRequired, 0)) AS DECIMAL(5,2)) AS ProgressPercent
    FROM [app].[Wallets] W
    INNER JOIN [app].[Users] U ON U.Id = W.UserId
    INNER JOIN [app].[KodyBonusowe] KB ON KB.Id = W.ActiveBonusId
    WHERE W.ActiveBonusId IS NOT NULL
      AND (W.BonusExpiresAt IS NULL OR W.BonusExpiresAt > SYSUTCDATETIME())
);
GO

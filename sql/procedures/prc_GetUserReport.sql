-- ============================================================
-- PROCEDURA: prc.GetUserReport
-- Opis: Raport aktywnosci uzytkownika (do panelu admina)
-- ============================================================
CREATE OR ALTER PROCEDURE [prc].[GetUserReport]
    @UserId         INT,
    @DateFrom       DATETIME2 = NULL,
    @DateTo         DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @DateFrom IS NULL SET @DateFrom = DATEADD(DAY, -30, SYSUTCDATETIME());
    IF @DateTo IS NULL SET @DateTo = SYSUTCDATETIME();

    SELECT
        U.Id               AS UserId,
        U.Nazwa             AS Username,
        U.Email,
        U.DataRejestracji   AS RegisteredAt,
        W.BalanceReal,
        W.BalanceBonus,
        (SELECT COUNT(*) FROM [app].[LoginHistories] LH WHERE LH.UserId = U.Id AND LH.LoggedAt BETWEEN @DateFrom AND @DateTo) AS LoginCount,
        (SELECT COUNT(*) FROM [app].[BetRecords] BR WHERE BR.UserId = U.Id AND BR.CreatedAt BETWEEN @DateFrom AND @DateTo) AS BetCount,
        (SELECT ISNULL(SUM(BR.Amount), 0) FROM [app].[BetRecords] BR WHERE BR.UserId = U.Id AND BR.CreatedAt BETWEEN @DateFrom AND @DateTo) AS TotalBetAmount,
        (SELECT ISNULL(SUM(BR.PayoutAmount), 0) FROM [app].[BetRecords] BR WHERE BR.UserId = U.Id AND BR.CreatedAt BETWEEN @DateFrom AND @DateTo AND BR.Settled = 1) AS TotalPayout,
        (SELECT ISNULL(SUM(SP.Amount), 0) FROM [app].[StripePayments] SP WHERE SP.UserId = U.Id AND SP.Status = 'completed' AND SP.CreatedAt BETWEEN @DateFrom AND @DateTo) AS TotalDeposits,
        (SELECT ISNULL(SUM(SW.Amount), 0) FROM [app].[StripeWithdrawals] SW WHERE SW.UserId = U.Id AND SW.Status = 'completed' AND SW.CreatedAt BETWEEN @DateFrom AND @DateTo) AS TotalWithdrawals
    FROM [app].[Users] U
    INNER JOIN [app].[Wallets] W ON W.UserId = U.Id
    WHERE U.Id = @UserId;
END;
GO

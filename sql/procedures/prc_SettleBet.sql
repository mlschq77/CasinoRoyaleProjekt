-- ============================================================
-- PROCEDURA: prc.SettleBet
-- Opis: Rozlicza zaklad - dodaje wygrana do salda
-- ============================================================
CREATE OR ALTER PROCEDURE [prc].[SettleBet]
    @BetRecordId    INT,
    @PayoutAmount   DECIMAL(18,2),
    @IsWin          BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    -- Pobierz dane zakladu
    DECLARE @UserId INT, @Amount DECIMAL(18,2), @Settled BIT;
    SELECT @UserId = UserId, @Amount = Amount, @Settled = Settled
    FROM [app].[BetRecords] WHERE Id = @BetRecordId;

    IF @Settled = 1
    BEGIN
        ROLLBACK;
        THROW 50004, N'Zaklad juz rozliczony.', 1;
    END

    -- Oznacz jako rozliczony
    UPDATE [app].[BetRecords]
    SET Settled = 1,
        PayoutAmount = @PayoutAmount
    WHERE Id = @BetRecordId;

    -- Dodaj wygrana do salda
    IF @IsWin = 1 AND @PayoutAmount > 0
    BEGIN
        -- Wplac wygrana na saldo real
        UPDATE [app].[Wallets]
        SET BalanceReal = BalanceReal + @PayoutAmount
        WHERE UserId = @UserId;

        -- Sprawdz czy wagering zostal spelniony
        UPDATE [app].[Wallets]
        SET BalanceBonus = 0,
            ActiveBonusId = NULL,
            WageringRequired = NULL,
            WageringProgress = NULL,
            BonusExpiresAt = NULL
        WHERE UserId = @UserId
          AND ActiveBonusId IS NOT NULL
          AND WageringProgress >= WageringRequired;
    END

    INSERT INTO [app].[GameResults] ([UserId], [GameKey], [GameName], [BetAmount], [PayoutAmount], [WinAmount])
    SELECT @UserId, SessionKey, GameName, Amount, @PayoutAmount,
           CASE WHEN @IsWin = 1 THEN @PayoutAmount ELSE 0 END
    FROM [app].[BetRecords] WHERE Id = @BetRecordId;

    COMMIT TRANSACTION;
END;
GO

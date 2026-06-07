-- ============================================================
-- PROCEDURA: prc.PlaceBet
-- Opis: Rejestruje zaklad i obniza saldo uzytkownika
-- ============================================================
CREATE OR ALTER PROCEDURE [prc].[PlaceBet]
    @UserId         INT,
    @Amount         DECIMAL(18,2),
    @GameName       NVARCHAR(32),
    @SessionKey     NVARCHAR(100) = NULL,
    @NewBetRecordId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    -- Sprawdz czy uzytkownik istnieje
    IF NOT EXISTS (SELECT 1 FROM [app].[Users] WHERE Id = @UserId)
    BEGIN
        ROLLBACK;
        THROW 50001, N'Uzytkownik nie istnieje.', 1;
    END

    -- Walidacja kwoty
    IF @Amount <= 0
    BEGIN
        ROLLBACK;
        THROW 50002, N'Kwota zakladu musi byc wieksza od 0.', 1;
    END

    -- Sprawdz saldo
    DECLARE @BalanceReal DECIMAL(18,2), @BalanceBonus DECIMAL(18,2);
    SELECT @BalanceReal = BalanceReal, @BalanceBonus = BalanceBonus
    FROM [app].[Wallets] WHERE UserId = @UserId;

    IF @BalanceReal + @BalanceBonus < @Amount
    BEGIN
        ROLLBACK;
        THROW 50003, N'Niewystarczajace srodki.', 1;
    END

    -- Odejmij od salda (najpierw bonus, potem real)
    DECLARE @FromBonus DECIMAL(18,2) = 0;

    IF @BalanceBonus >= @Amount
    BEGIN
        SET @FromBonus = @Amount;
        UPDATE [app].[Wallets] SET BalanceBonus = BalanceBonus - @Amount WHERE UserId = @UserId;
    END
    ELSE
    BEGIN
        SET @FromBonus = @BalanceBonus;
        UPDATE [app].[Wallets]
        SET BalanceBonus = 0,
            BalanceReal = BalanceReal - (@Amount - @BalanceBonus)
        WHERE UserId = @UserId;
    END

    -- Generuj SessionKey jesli nie podano
    IF @SessionKey IS NULL
        SET @SessionKey = LOWER(REPLACE(NEWID(), '-', ''));

    -- Wstaw rekord zakladu
    INSERT INTO [app].[BetRecords] ([UserId], [Amount], [AmountFromBonus], [SessionKey], [GameName])
    VALUES (@UserId, @Amount, @FromBonus, @SessionKey, @GameName);

    SET @NewBetRecordId = SCOPE_IDENTITY();

    -- Zaktualizuj wagering progress jesli aktywny bonus
    UPDATE [app].[Wallets]
    SET WageringProgress = WageringProgress + @Amount
    WHERE UserId = @UserId
      AND ActiveBonusId IS NOT NULL
      AND WageringProgress IS NOT NULL
      AND WageringRequired IS NOT NULL;

    COMMIT TRANSACTION;
END;
GO

-- ============================================================
-- PROCEDURA: prc.ApplyBonusCode
-- Opis: Aplikuje kod bonusowy do konta uzytkownika
-- ============================================================
CREATE OR ALTER PROCEDURE [prc].[ApplyBonusCode]
    @UserId         INT,
    @Kod            NVARCHAR(64),
    @DepositAmount  DECIMAL(18,2) = 0,
    @ResultMessage  NVARCHAR(500) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    -- Sprawdz czy kod istnieje i jest wazny
    DECLARE @KodId INT, @MinWplata DECIMAL(18,2), @BonusProc DECIMAL(5,2);
    DECLARE @BonusKwot DECIMAL(18,2), @WaznyDo DATETIME2, @WagMult DECIMAL(18,2);

    SELECT @KodId = Id, @MinWplata = MinimalnaWplata, @BonusProc = BonusProcentowy,
           @BonusKwot = BonusKwotowy, @WaznyDo = WaznyDo, @WagMult = WageringMultiplier
    FROM [app].[KodyBonusowe] WHERE Kod = @Kod;

    IF @KodId IS NULL
    BEGIN
        SET @ResultMessage = N'Kod bonusowy nie istnieje.';
        ROLLBACK;
        RETURN;
    END

    IF @WaznyDo IS NOT NULL AND @WaznyDo < SYSUTCDATETIME()
    BEGIN
        SET @ResultMessage = N'Kod bonusowy wygasl.';
        ROLLBACK;
        RETURN;
    END

    -- Sprawdz czy uzytkownik juz uzytego kodu
    IF EXISTS (SELECT 1 FROM [app].[UzyteKodyBonusowe] WHERE UserId = @UserId AND KodBonusowyId = @KodId)
    BEGIN
        SET @ResultMessage = N'Ten kod bonusowy zostal juz uzyty.';
        ROLLBACK;
        RETURN;
    END

    -- Sprawdz minimalna wplate
    IF @DepositAmount < @MinWplata
    BEGIN
        SET @ResultMessage = N'Minimalna wplata dla tego kodu to ' + CAST(@MinWplata AS NVARCHAR(20)) + '.';
        ROLLBACK;
        RETURN;
    END

    -- Oblicz bonus
    DECLARE @BonusAmount DECIMAL(18,2) = @BonusKwot + (@DepositAmount * @BonusProc / 100);

    -- Dodaj bonus do salda
    UPDATE [app].[Wallets]
    SET BalanceBonus = BalanceBonus + @BonusAmount,
        ActiveBonusId = @KodId,
        WageringRequired = @BonusAmount * @WagMult,
        WageringProgress = 0,
        BonusExpiresAt = DATEADD(DAY, 30, SYSUTCDATETIME())
    WHERE UserId = @UserId;

    -- Zapisz uzycie kodu
    INSERT INTO [app].[UzyteKodyBonusowe] ([UserId], [KodBonusowyId], [BonusAmount])
    VALUES (@UserId, @KodId, @BonusAmount);

    SET @ResultMessage = N'Bonus zostal aktywowany: ' + CAST(@BonusAmount AS NVARCHAR(20)) + ' (wagering x' + CAST(@WagMult AS NVARCHAR(20)) + ')';

    COMMIT TRANSACTION;
END;
GO

-- ============================================================
-- TRIGGER: app.trg_Wallet_CheckBonusExpiry
-- Opis: Sprawdza czy bonus wygasl przed kazda operacja na Wallet
-- ============================================================
CREATE OR ALTER TRIGGER [app].[trg_Wallet_CheckBonusExpiry]
ON [app].[Wallets]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Wygas bonus jesli minela data waznosci
    UPDATE [app].[Wallets]
    SET ActiveBonusId = NULL,
        BalanceBonus = 0,
        WageringRequired = NULL,
        WageringProgress = NULL,
        BonusExpiresAt = NULL
    WHERE Id IN (SELECT Id FROM inserted)
      AND ActiveBonusId IS NOT NULL
      AND BonusExpiresAt IS NOT NULL
      AND BonusExpiresAt < SYSUTCDATETIME();
END;
GO

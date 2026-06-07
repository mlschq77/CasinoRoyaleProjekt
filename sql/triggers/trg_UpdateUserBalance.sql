-- ============================================================
-- TRIGGER: app.trg_BetRecord_UpdateBalance
-- Opis: Automatycznie aktualizuje GameResult po wstawieniu BetRecord
-- ============================================================
CREATE OR ALTER TRIGGER [app].[trg_BetRecord_UpdateBalance]
ON [app].[BetRecords]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Rejestruj rozpoczecie gry w GameResults z domyslnymi wartosciami
    INSERT INTO [app].[GameResults] ([UserId], [GameKey], [GameName], [BetAmount], [PayoutAmount], [WinAmount])
    SELECT
        i.UserId,
        i.SessionKey,
        i.GameName,
        i.Amount,
        0,
        0
    FROM inserted i
    WHERE NOT EXISTS (
        SELECT 1 FROM [app].[GameResults] gr
        WHERE gr.UserId = i.UserId AND gr.GameKey = i.SessionKey
    );
END;
GO

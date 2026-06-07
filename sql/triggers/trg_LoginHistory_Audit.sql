-- ============================================================
-- TRIGGER: audit.trg_LoginHistory_Audit
-- Opis: Loguje nieudane proby logowania do tabeli auditowej
-- ============================================================
CREATE OR ALTER TRIGGER [audit].[trg_LoginHistory_Audit]
ON [app].[LoginHistories]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Jesli nieudane logowanie, zapisz do audytu
    INSERT INTO [audit].[FailedLogins] ([UserId], [IpAddress], [AttemptedAt], [UserAgent])
    SELECT
        i.UserId,
        i.IpAddress,
        i.LoggedAt,
        i.UserAgent
    FROM inserted i
    WHERE i.Successful = 0
      AND i.EventType = 'failed_login';
END;
GO

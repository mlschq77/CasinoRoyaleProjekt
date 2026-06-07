-- ============================================================
-- Tabele auditowe i security - Casino Royale
-- ============================================================
USE [CasinoDB];
GO

-- ============================================================
-- Tabela auditowa: audit.FailedLogins
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FailedLogins' AND schema_id = SCHEMA_ID('audit'))
BEGIN
    CREATE TABLE [audit].[FailedLogins] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [UserId]        INT             NULL,
        [IpAddress]     NVARCHAR(45)    NOT NULL DEFAULT '',
        [AttemptedAt]   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [UserAgent]     NVARCHAR(500)   NOT NULL DEFAULT '',
        CONSTRAINT [PK_FailedLogins] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

-- ============================================================
-- Tabela auditowa: audit.DataChanges
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DataChanges' AND schema_id = SCHEMA_ID('audit'))
BEGIN
    CREATE TABLE [audit].[DataChanges] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [TableName]     NVARCHAR(128)   NOT NULL,
        [Action]        NVARCHAR(10)    NOT NULL, -- INSERT, UPDATE, DELETE
        [RecordId]      INT             NOT NULL,
        [ChangedBy]     INT             NULL,
        [OldValues]     NVARCHAR(MAX)   NULL,
        [NewValues]     NVARCHAR(MAX)   NULL,
        [ChangedAt]     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_DataChanges] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

-- Indeks dla DataChanges
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DataChanges_TableName_ChangedAt')
    CREATE NONCLUSTERED INDEX [IX_DataChanges_TableName_ChangedAt]
        ON [audit].[DataChanges] ([TableName], [ChangedAt] DESC);
GO

-- ============================================================
-- Tabela: security.ApiKeys
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiKeys' AND schema_id = SCHEMA_ID('security'))
BEGIN
    CREATE TABLE [security].[ApiKeys] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [UserId]        INT             NOT NULL,
        [ApiKeyHash]    NVARCHAR(256)   NOT NULL,
        [Name]          NVARCHAR(100)   NOT NULL DEFAULT '',
        [IsActive]      BIT             NOT NULL DEFAULT 1,
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [ExpiresAt]     DATETIME2       NULL,
        [LastUsedAt]    DATETIME2       NULL,
        CONSTRAINT [PK_ApiKeys] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_ApiKeys_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Widok: report.UserSummary
-- ============================================================
CREATE OR ALTER VIEW [report].[UserSummary]
AS
SELECT
    U.Id,
    U.Nazwa,
    U.Email,
    U.DataRejestracji,
    U.KycStatus,
    U.IsAdmin,
    W.BalanceReal,
    W.BalanceBonus,
    (SELECT ISNULL(SUM(Amount), 0) FROM [app].[BetRecords] WHERE UserId = U.Id AND Settled = 1) AS TotalBets,
    (SELECT ISNULL(SUM(PayoutAmount), 0) FROM [app].[BetRecords] WHERE UserId = U.Id AND Settled = 1) AS TotalWins,
    (SELECT COUNT(*) FROM [app].[LoginHistories] WHERE UserId = U.Id AND Successful = 1) AS SuccessfulLogins,
    (SELECT COUNT(*) FROM [app].[LoginHistories] WHERE UserId = U.Id AND Successful = 0) AS FailedLogins
FROM [app].[Users] U
INNER JOIN [app].[Wallets] W ON W.UserId = U.Id;
GO

-- Nadaj uprawnienia SELECT na widok dla roli readonly
GRANT SELECT ON [report].[UserSummary] TO [CasinoReadOnly];
GO

PRINT 'Tabele auditowe i security utworzone.';
GO

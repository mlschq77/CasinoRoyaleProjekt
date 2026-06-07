-- ============================================================
-- TWORZENIE TABEL - Casino Royale
-- ============================================================
USE [CasinoDB];
GO

-- ============================================================
-- Tabela: app.Users
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[Users] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [Imie]              NVARCHAR(50)    NOT NULL,
        [Nazwisko]          NVARCHAR(50)    NOT NULL,
        [Nazwa]             NVARCHAR(50)    NOT NULL,
        [Email]             NVARCHAR(450)   NOT NULL,
        [HasloHash]         NVARCHAR(MAX)   NOT NULL,
        [IsAdmin]           BIT             NOT NULL DEFAULT 0,
        [DataRejestracji]   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [KycStatus]         INT             NOT NULL DEFAULT 0,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
    );
END
GO

-- ============================================================
-- Tabela: app.Wallets
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Wallets' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[Wallets] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [UserId]            INT             NOT NULL,
        [BalanceReal]       DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [BalanceBonus]      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [ActiveBonusId]     INT             NULL,
        [WageringRequired]  DECIMAL(18,2)   NULL,
        [WageringProgress]  DECIMAL(18,2)   NULL,
        [BonusExpiresAt]    DATETIME2       NULL,
        CONSTRAINT [PK_Wallets] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Wallets_UserId] UNIQUE ([UserId]),
        CONSTRAINT [FK_Wallets_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.BetRecords
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BetRecords' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[BetRecords] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [UserId]            INT             NOT NULL,
        [Amount]            DECIMAL(18,2)   NOT NULL,
        [AmountFromBonus]   DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [BonusDeductions]   NVARCHAR(500)   NULL,
        [SessionKey]        NVARCHAR(100)   NOT NULL,
        [Settled]           BIT             NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [GameName]          NVARCHAR(32)    NOT NULL,
        [PayoutAmount]      DECIMAL(18,2)   NULL,
        CONSTRAINT [PK_BetRecords] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_BetRecords_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.LoginHistories
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LoginHistories' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[LoginHistories] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [UserId]        INT             NOT NULL,
        [IpAddress]     NVARCHAR(45)    NOT NULL DEFAULT '',
        [UserAgent]     NVARCHAR(500)   NOT NULL DEFAULT '',
        [Successful]    BIT             NOT NULL DEFAULT 1,
        [EventType]     NVARCHAR(20)    NOT NULL DEFAULT 'login',
        [LoggedAt]      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_LoginHistories] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_LoginHistories_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.KodyBonusowe
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KodyBonusowe' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[KodyBonusowe] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [Kod]               NVARCHAR(64)    NOT NULL,
        [MinimalnaWplata]   DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [BonusProcentowy]   DECIMAL(5,2)    NOT NULL DEFAULT 0,
        [BonusKwotowy]      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [Utworzono]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [WaznyDo]           DATETIME2       NULL,
        [WageringMultiplier] DECIMAL(18,2)  NOT NULL DEFAULT 20,
        CONSTRAINT [PK_KodyBonusowe] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_KodyBonusowe_Kod] UNIQUE ([Kod])
    );
END
GO

-- ============================================================
-- Tabela: app.UzyteKodyBonusowe
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UzyteKodyBonusowe' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[UzyteKodyBonusowe] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [UserId]            INT             NOT NULL,
        [KodBonusowyId]     INT             NOT NULL,
        [StripePaymentId]   INT             NULL,
        [BonusAmount]       DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [SessionId]         NVARCHAR(450)   NOT NULL DEFAULT '',
        [UzytyW]            DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UzyteKodyBonusowe] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_UzyteKodyBonusowe_UserKod] UNIQUE ([UserId], [KodBonusowyId]),
        CONSTRAINT [FK_UzyteKodyBonusowe_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UzyteKodyBonusowe_Kody] FOREIGN KEY ([KodBonusowyId])
            REFERENCES [app].[KodyBonusowe]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.StripePayments
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StripePayments' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[StripePayments] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [UserId]        INT             NOT NULL,
        [Amount]        DECIMAL(18,2)   NOT NULL,
        [Currency]      NVARCHAR(10)    NOT NULL DEFAULT 'USD',
        [SessionId]     NVARCHAR(450)   NOT NULL,
        [Status]        NVARCHAR(50)    NOT NULL DEFAULT 'pending',
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [CompletedAt]   DATETIME2       NULL,
        CONSTRAINT [PK_StripePayments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_StripePayments_SessionId] UNIQUE ([SessionId]),
        CONSTRAINT [FK_StripePayments_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id])
    );
END
GO

-- ============================================================
-- Tabela: app.StripeWithdrawals
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StripeWithdrawals' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[StripeWithdrawals] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [UserId]        INT             NOT NULL,
        [Amount]        DECIMAL(18,2)   NOT NULL,
        [Currency]      NVARCHAR(10)    NOT NULL DEFAULT 'USD',
        [TransferId]    NVARCHAR(450)   NOT NULL,
        [Status]        NVARCHAR(50)    NOT NULL DEFAULT 'pending',
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [CompletedAt]   DATETIME2       NULL,
        CONSTRAINT [PK_StripeWithdrawals] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_StripeWithdrawals_TransferId] UNIQUE ([TransferId]),
        CONSTRAINT [FK_StripeWithdrawals_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id])
    );
END
GO

-- ============================================================
-- Tabela: app.KycDocuments
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KycDocuments' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[KycDocuments] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [UserId]            INT             NOT NULL,
        [DocumentType]      INT             NOT NULL,
        [FileName]          NVARCHAR(255)   NOT NULL,
        [ContentType]       NVARCHAR(100)   NOT NULL,
        [StoragePath]       NVARCHAR(500)   NOT NULL,
        [Status]            INT             NOT NULL DEFAULT 0,
        [UploadedAt]        DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [AdminComment]      NVARCHAR(1000)  NULL,
        [ReviewedByUserId]  INT             NULL,
        [ReviewedAt]        DATETIME2       NULL,
        CONSTRAINT [PK_KycDocuments] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_KycDocuments_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.AutomatProviderzy
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AutomatProviderzy' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[AutomatProviderzy] (
        [Id]    INT             IDENTITY(1,1)   NOT NULL,
        [Nazwa] NVARCHAR(450)   NOT NULL,
        CONSTRAINT [PK_AutomatProviderzy] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_AutomatProviderzy_Nazwa] UNIQUE ([Nazwa])
    );
END
GO

-- ============================================================
-- Tabela: app.Kategorie
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Kategorie' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[Kategorie] (
        [Id]    INT             IDENTITY(1,1)   NOT NULL,
        [Nazwa] NVARCHAR(450)   NOT NULL,
        CONSTRAINT [PK_Kategorie] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [UQ_Kategorie_Nazwa] UNIQUE ([Nazwa])
    );
END
GO

-- ============================================================
-- Tabela: app.AutomatyInfo
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AutomatyInfo' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[AutomatyInfo] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [Nazwa]         NVARCHAR(MAX)   NOT NULL,
        [ProviderId]    INT             NULL,
        CONSTRAINT [PK_AutomatyInfo] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_AutomatyInfo_Provider] FOREIGN KEY ([ProviderId])
            REFERENCES [app].[AutomatProviderzy]([Id]) ON DELETE SET NULL
    );
END
GO

-- ============================================================
-- Tabela: app.AutomatyKategorie (M:N)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AutomatyKategorie' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[AutomatyKategorie] (
        [AutomatId]     INT NOT NULL,
        [KategoriaId]   INT NOT NULL,
        CONSTRAINT [PK_AutomatyKategorie] PRIMARY KEY CLUSTERED ([AutomatId], [KategoriaId]),
        CONSTRAINT [FK_AutomatyKategorie_Automat] FOREIGN KEY ([AutomatId])
            REFERENCES [app].[AutomatyInfo]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AutomatyKategorie_Kategoria] FOREIGN KEY ([KategoriaId])
            REFERENCES [app].[Kategorie]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.GameResults (archiwum)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GameResults' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[GameResults] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [UserId]        INT             NOT NULL,
        [GameKey]       NVARCHAR(MAX)   NOT NULL DEFAULT '',
        [GameName]      NVARCHAR(MAX)   NOT NULL DEFAULT '',
        [BetAmount]     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [PayoutAmount]  DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [WinAmount]     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [PlayedAtUtc]   DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [MinesGameId]   INT             NULL,
        CONSTRAINT [PK_GameResults] PRIMARY KEY CLUSTERED ([Id])
    );
END
GO

-- Gry (tabele specyficzne dla gier)
-- ============================================================
-- Tabela: app.MinesGames
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MinesGames' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[MinesGames] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [UserId]            INT             NOT NULL,
        [GridSize]          INT             NOT NULL DEFAULT 25,
        [MineCount]         INT             NOT NULL,
        [MinePositions]     NVARCHAR(MAX)   NOT NULL DEFAULT '',
        [RevealedPositions] NVARCHAR(MAX)   NOT NULL DEFAULT '',
        [BetAmount]         DECIMAL(18,2)   NOT NULL,
        [WinAmount]         DECIMAL(18,2)   NULL,
        [IsActive]          BIT             NOT NULL DEFAULT 1,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_MinesGames] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_MinesGames_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.PlinkoGames
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlinkoGames' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[PlinkoGames] (
        [Id]        INT             IDENTITY(1,1)   NOT NULL,
        [UserId]    INT             NOT NULL,
        [BetAmount] DECIMAL(18,2)   NOT NULL,
        [Risk]      NVARCHAR(50)    NOT NULL DEFAULT 'medium',
        [WinAmount] DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_PlinkoGames] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_PlinkoGames_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.BlackjackGames
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BlackjackGames' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[BlackjackGames] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [UserId]            INT             NOT NULL,
        [DeckJson]          NVARCHAR(MAX)   NOT NULL DEFAULT '[]',
        [PlayerHandJson]    NVARCHAR(MAX)   NOT NULL DEFAULT '[]',
        [DealerHandJson]    NVARCHAR(MAX)   NOT NULL DEFAULT '[]',
        [SplitHandJson]     NVARCHAR(MAX)   NOT NULL DEFAULT '[]',
        [BetAmount]         DECIMAL(18,2)   NOT NULL,
        [SplitBetAmount]    DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [IsActive]          BIT             NOT NULL DEFAULT 1,
        [IsPlayerTurn]      BIT             NOT NULL DEFAULT 1,
        [IsSplitActive]     BIT             NOT NULL DEFAULT 0,
        [IsPlayingSplitHand] BIT            NOT NULL DEFAULT 0,
        [PayoutProcessed]   BIT             NOT NULL DEFAULT 0,
        [Result]            NVARCHAR(50)    NULL,
        [WinAmount]         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [FinishedAt]        DATETIME2       NULL,
        CONSTRAINT [PK_BlackjackGames] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_BlackjackGames_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.CrashSessions
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CrashSessions' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[CrashSessions] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [UserId]            INT             NOT NULL,
        [BetAmount]         DECIMAL(18,2)   NOT NULL,
        [CrashPoint]        DECIMAL(18,2)   NOT NULL,
        [CashoutMultiplier] DECIMAL(18,2)   NULL,
        [WinAmount]         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [StartTime]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        [IsActive]          BIT             NOT NULL DEFAULT 1,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_CrashSessions] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_CrashSessions_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.RouletteGames
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RouletteGames' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[RouletteGames] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [UserId]        INT             NOT NULL,
        [BetAmount]     DECIMAL(18,2)   NOT NULL,
        [BetType]       NVARCHAR(50)    NOT NULL DEFAULT '',
        [BetValue]      NVARCHAR(50)    NOT NULL DEFAULT '',
        [ResultNumber]  INT             NOT NULL DEFAULT 0,
        [WinAmount]     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [BetsJson]      NVARCHAR(MAX)   NOT NULL DEFAULT '[]',
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_RouletteGames] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_RouletteGames_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.DiceGames
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DiceGames' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[DiceGames] (
        [Id]        INT             IDENTITY(1,1)   NOT NULL,
        [UserId]    INT             NOT NULL,
        [BetAmount] DECIMAL(18,2)   NOT NULL,
        [Mode]      NVARCHAR(10)    NOT NULL DEFAULT 'over',
        [Target]    INT             NOT NULL DEFAULT 50,
        [Roll]      INT             NOT NULL DEFAULT 1,
        [Multiplier] DECIMAL(18,2)  NOT NULL DEFAULT 1,
        [WinAmount] DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_DiceGames] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_DiceGames_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.KenoGames
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KenoGames' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[KenoGames] (
        [Id]                INT             IDENTITY(1,1)   NOT NULL,
        [UserId]            INT             NOT NULL,
        [BetAmount]         DECIMAL(18,2)   NOT NULL,
        [SelectedNumbers]   NVARCHAR(MAX)   NOT NULL DEFAULT '',
        [DrawnNumbers]      NVARCHAR(MAX)   NOT NULL DEFAULT '',
        [Hits]              INT             NOT NULL DEFAULT 0,
        [Multiplier]        DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [WinAmount]         DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [CreatedAt]         DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_KenoGames] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_KenoGames_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

-- ============================================================
-- Tabela: app.BaccaratGames
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BaccaratGames' AND schema_id = SCHEMA_ID('app'))
BEGIN
    CREATE TABLE [app].[BaccaratGames] (
        [Id]            INT             IDENTITY(1,1)   NOT NULL,
        [UserId]        INT             NOT NULL,
        [DeckJson]      NVARCHAR(MAX)   NOT NULL DEFAULT '[]',
        [PlayerHandJson] NVARCHAR(MAX)  NOT NULL DEFAULT '[]',
        [BankerHandJson] NVARCHAR(MAX)  NOT NULL DEFAULT '[]',
        [BetAmount]     DECIMAL(18,2)   NOT NULL,
        [BetType]       NVARCHAR(20)    NOT NULL DEFAULT 'player',
        [PlayerValue]   INT             NOT NULL DEFAULT 0,
        [BankerValue]   INT             NOT NULL DEFAULT 0,
        [Result]        NVARCHAR(50)    NOT NULL DEFAULT '',
        [WinAmount]     DECIMAL(18,2)   NOT NULL DEFAULT 0,
        [IsActive]      BIT             NOT NULL DEFAULT 1,
        [CreatedAt]     DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_BaccaratGames] PRIMARY KEY CLUSTERED ([Id]),
        CONSTRAINT [FK_BaccaratGames_Users] FOREIGN KEY ([UserId])
            REFERENCES [app].[Users]([Id]) ON DELETE CASCADE
    );
END
GO

PRINT 'Tabele zostaly utworzone pomyslnie.';
GO

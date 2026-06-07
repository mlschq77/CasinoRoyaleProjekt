-- ============================================================
-- INDEKSY - Casino Royale
-- ============================================================
USE [CasinoDB];
GO

-- ============================================================
-- Indeksy dla Users
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email')
    CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [app].[Users] ([Email])
        INCLUDE ([Id], [Nazwa], [HasloHash], [IsAdmin]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Nazwa')
    CREATE NONCLUSTERED INDEX [IX_Users_Nazwa] ON [app].[Users] ([Nazwa])
        INCLUDE ([Id], [Email]);
GO

-- ============================================================
-- Indeksy dla BetRecords
-- ============================================================
-- Indeks złożony dla paginacji i filtrowania (już istnieje w EF)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BetRecords_UserId_SessionKey_Settled')
    CREATE NONCLUSTERED INDEX [IX_BetRecords_UserId_SessionKey_Settled]
        ON [app].[BetRecords] ([UserId], [SessionKey], [Settled])
        INCLUDE ([Amount], [PayoutAmount], [GameName], [CreatedAt]);
GO

-- Indeks dla historii zakładów użytkownika (paginacja po dacie)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BetRecords_UserId_CreatedAt')
    CREATE NONCLUSTERED INDEX [IX_BetRecords_UserId_CreatedAt]
        ON [app].[BetRecords] ([UserId], [CreatedAt] DESC)
        INCLUDE ([Amount], [PayoutAmount], [GameName], [Settled]);
GO

-- Indeks dla gier (filtrowanie po GameName)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BetRecords_GameName')
    CREATE NONCLUSTERED INDEX [IX_BetRecords_GameName]
        ON [app].[BetRecords] ([GameName])
        INCLUDE ([UserId], [Amount], [PayoutAmount], [CreatedAt]);
GO

-- ============================================================
-- Indeksy dla LoginHistories
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginHistories_UserId_LoggedAt')
    CREATE NONCLUSTERED INDEX [IX_LoginHistories_UserId_LoggedAt]
        ON [app].[LoginHistories] ([UserId], [LoggedAt] DESC)
        INCLUDE ([IpAddress], [Successful], [EventType]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LoginHistories_LoggedAt')
    CREATE NONCLUSTERED INDEX [IX_LoginHistories_LoggedAt]
        ON [app].[LoginHistories] ([LoggedAt] DESC);
GO

-- ============================================================
-- Indeksy dla Wallet
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Wallets_Balance')
    CREATE NONCLUSTERED INDEX [IX_Wallets_Balance]
        ON [app].[Wallets] ([BalanceReal] DESC)
        WHERE [BalanceReal] > 0;
GO

-- ============================================================
-- Indeksy dla StripePayments
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StripePayments_UserId')
    CREATE NONCLUSTERED INDEX [IX_StripePayments_UserId]
        ON [app].[StripePayments] ([UserId], [CreatedAt] DESC);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StripePayments_Status')
    CREATE NONCLUSTERED INDEX [IX_StripePayments_Status]
        ON [app].[StripePayments] ([Status])
        INCLUDE ([UserId], [Amount]);
GO

-- ============================================================
-- Indeksy dla StripeWithdrawals
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StripeWithdrawals_UserId')
    CREATE NONCLUSTERED INDEX [IX_StripeWithdrawals_UserId]
        ON [app].[StripeWithdrawals] ([UserId], [CreatedAt] DESC);
GO

-- ============================================================
-- Indeksy dla GameResults (archiwum)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GameResults_UserId_PlayedAt')
    CREATE NONCLUSTERED INDEX [IX_GameResults_UserId_PlayedAt]
        ON [app].[GameResults] ([UserId], [PlayedAtUtc] DESC)
        INCLUDE ([GameName], [BetAmount], [WinAmount]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GameResults_PlayedAt')
    CREATE NONCLUSTERED INDEX [IX_GameResults_PlayedAt]
        ON [app].[GameResults] ([PlayedAtUtc] DESC);
GO

-- ============================================================
-- Indeksy dla KYC
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KycDocuments_UserId')
    CREATE NONCLUSTERED INDEX [IX_KycDocuments_UserId]
        ON [app].[KycDocuments] ([UserId], [Status]);
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KycDocuments_Status')
    CREATE NONCLUSTERED INDEX [IX_KycDocuments_Status]
        ON [app].[KycDocuments] ([Status])
        INCLUDE ([UserId], [DocumentType]);
GO

-- ============================================================
-- Indeksy dla gier (UserId + CreatedAt)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_MinesGames_UserId')
    CREATE NONCLUSTERED INDEX [IX_MinesGames_UserId] ON [app].[MinesGames] ([UserId], [CreatedAt] DESC);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PlinkoGames_UserId')
    CREATE NONCLUSTERED INDEX [IX_PlinkoGames_UserId] ON [app].[PlinkoGames] ([UserId], [CreatedAt] DESC);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BlackjackGames_UserId')
    CREATE NONCLUSTERED INDEX [IX_BlackjackGames_UserId] ON [app].[BlackjackGames] ([UserId], [CreatedAt] DESC);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CrashSessions_UserId')
    CREATE NONCLUSTERED INDEX [IX_CrashSessions_UserId] ON [app].[CrashSessions] ([UserId], [CreatedAt] DESC);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RouletteGames_UserId')
    CREATE NONCLUSTERED INDEX [IX_RouletteGames_UserId] ON [app].[RouletteGames] ([UserId], [CreatedAt] DESC);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DiceGames_UserId')
    CREATE NONCLUSTERED INDEX [IX_DiceGames_UserId] ON [app].[DiceGames] ([UserId], [CreatedAt] DESC);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KenoGames_UserId')
    CREATE NONCLUSTERED INDEX [IX_KenoGames_UserId] ON [app].[KenoGames] ([UserId], [CreatedAt] DESC);
GO
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BaccaratGames_UserId')
    CREATE NONCLUSTERED INDEX [IX_BaccaratGames_UserId] ON [app].[BaccaratGames] ([UserId], [CreatedAt] DESC);
GO

PRINT 'Indeksy zostaly utworzone pomyslnie.';
GO

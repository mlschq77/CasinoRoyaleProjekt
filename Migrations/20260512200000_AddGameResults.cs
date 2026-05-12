using CasinoRoyale.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    [DbContext(typeof(Automaty))]
    [Migration("20260512200000_AddGameResults")]
    public partial class AddGameResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[GameResults]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[GameResults] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [GameKey] nvarchar(32) NOT NULL,
        [BetAmount] decimal(18,2) NOT NULL,
        [PayoutAmount] decimal(18,2) NOT NULL,
        [PlayedAtUtc] datetime2 NOT NULL,
        [MinesGameId] int NULL,
        CONSTRAINT [PK_GameResults] PRIMARY KEY ([Id])
    );
END
");

            // Kazdy Sql() = osobny batch — SQL Server nie widzi nowych kolumn w tym samym batchu co ALTER ADD.
            var t = "OBJECT_ID(N'[dbo].[GameResults]', N'U') IS NOT NULL";

            migrationBuilder.Sql($@"
IF {t} AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'UserId')
    ALTER TABLE [dbo].[GameResults] ADD [UserId] int NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND COL_LENGTH('dbo.GameResults', 'UserId') IS NOT NULL
    UPDATE [dbo].[GameResults] SET [UserId] = 0 WHERE [UserId] IS NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'UserId' AND is_nullable = 1)
    ALTER TABLE [dbo].[GameResults] ALTER COLUMN [UserId] int NOT NULL;
");

            migrationBuilder.Sql($@"
IF {t} AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'GameKey')
    ALTER TABLE [dbo].[GameResults] ADD [GameKey] nvarchar(32) NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND COL_LENGTH('dbo.GameResults', 'GameKey') IS NOT NULL
    UPDATE [dbo].[GameResults] SET [GameKey] = N'' WHERE [GameKey] IS NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'GameKey' AND is_nullable = 1)
    ALTER TABLE [dbo].[GameResults] ALTER COLUMN [GameKey] nvarchar(32) NOT NULL;
");

            migrationBuilder.Sql($@"
IF {t} AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'BetAmount')
    ALTER TABLE [dbo].[GameResults] ADD [BetAmount] decimal(18,2) NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND COL_LENGTH('dbo.GameResults', 'BetAmount') IS NOT NULL
    UPDATE [dbo].[GameResults] SET [BetAmount] = 0 WHERE [BetAmount] IS NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'BetAmount' AND is_nullable = 1)
    ALTER TABLE [dbo].[GameResults] ALTER COLUMN [BetAmount] decimal(18,2) NOT NULL;
");

            migrationBuilder.Sql($@"
IF {t} AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'PayoutAmount')
    ALTER TABLE [dbo].[GameResults] ADD [PayoutAmount] decimal(18,2) NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND COL_LENGTH('dbo.GameResults', 'PayoutAmount') IS NOT NULL
    UPDATE [dbo].[GameResults] SET [PayoutAmount] = 0 WHERE [PayoutAmount] IS NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'PayoutAmount' AND is_nullable = 1)
    ALTER TABLE [dbo].[GameResults] ALTER COLUMN [PayoutAmount] decimal(18,2) NOT NULL;
");

            migrationBuilder.Sql($@"
IF {t} AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'PlayedAtUtc')
    ALTER TABLE [dbo].[GameResults] ADD [PlayedAtUtc] datetime2 NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND COL_LENGTH('dbo.GameResults', 'PlayedAtUtc') IS NOT NULL
   AND COL_LENGTH('dbo.GameResults', 'PlayedAt') IS NOT NULL
    EXEC(N'UPDATE [dbo].[GameResults] SET [PlayedAtUtc] = TRY_CONVERT(datetime2, [PlayedAt]) WHERE [PlayedAtUtc] IS NULL');
");
            migrationBuilder.Sql($@"
IF {t} AND COL_LENGTH('dbo.GameResults', 'PlayedAtUtc') IS NOT NULL
   AND COL_LENGTH('dbo.GameResults', 'CreatedAtUtc') IS NOT NULL
    EXEC(N'UPDATE [dbo].[GameResults] SET [PlayedAtUtc] = TRY_CONVERT(datetime2, [CreatedAtUtc]) WHERE [PlayedAtUtc] IS NULL');
");
            migrationBuilder.Sql($@"
IF {t} AND COL_LENGTH('dbo.GameResults', 'PlayedAtUtc') IS NOT NULL
   AND COL_LENGTH('dbo.GameResults', 'CreatedAt') IS NOT NULL
    EXEC(N'UPDATE [dbo].[GameResults] SET [PlayedAtUtc] = TRY_CONVERT(datetime2, [CreatedAt]) WHERE [PlayedAtUtc] IS NULL');
");
            migrationBuilder.Sql($@"
IF {t} AND COL_LENGTH('dbo.GameResults', 'PlayedAtUtc') IS NOT NULL
    UPDATE [dbo].[GameResults] SET [PlayedAtUtc] = SYSUTCDATETIME() WHERE [PlayedAtUtc] IS NULL;
");
            migrationBuilder.Sql($@"
IF {t} AND EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'PlayedAtUtc' AND is_nullable = 1)
    ALTER TABLE [dbo].[GameResults] ALTER COLUMN [PlayedAtUtc] datetime2 NOT NULL;
");

            migrationBuilder.Sql($@"
IF {t} AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GameResults]') AND name = N'MinesGameId')
    ALTER TABLE [dbo].[GameResults] ADD [MinesGameId] int NULL;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[GameResults]', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_GameResults_UserId'
          AND object_id = OBJECT_ID(N'[dbo].[GameResults]'))
    CREATE NONCLUSTERED INDEX [IX_GameResults_UserId] ON [dbo].[GameResults] ([UserId]);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[GameResults]', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.GameResults', 'PlayedAtUtc') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_GameResults_UserId_PlayedAtUtc'
          AND object_id = OBJECT_ID(N'[dbo].[GameResults]'))
    CREATE NONCLUSTERED INDEX [IX_GameResults_UserId_PlayedAtUtc] ON [dbo].[GameResults] ([UserId], [PlayedAtUtc]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[GameResults]', N'U') IS NOT NULL
    DROP TABLE [dbo].[GameResults];
");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasinoRoyale.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dodanie kolumny BetsJson do RouletteGames, jeśli nie istnieje
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[RouletteGames]')
                    AND name = N'BetsJson'
                )
                BEGIN
                    ALTER TABLE [dbo].[RouletteGames] ADD [BetsJson] nvarchar(max) NULL;
                END
            ");

            // Utworzenie tabeli BaccaratGames, jeśli nie istnieje
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[BaccaratGames]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[BaccaratGames] (
                        [Id] int NOT NULL IDENTITY,
                        [UserId] int NOT NULL,
                        [DeckJson] nvarchar(max) NOT NULL,
                        [PlayerHandJson] nvarchar(max) NOT NULL,
                        [BankerHandJson] nvarchar(max) NOT NULL,
                        [BetAmount] decimal(18,2) NOT NULL,
                        [BetType] nvarchar(max) NOT NULL,
                        [PlayerValue] int NOT NULL,
                        [BankerValue] int NOT NULL,
                        [Result] nvarchar(max) NOT NULL,
                        [WinAmount] decimal(18,2) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        CONSTRAINT [PK_BaccaratGames] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cofamy tylko to, co mogliśmy dodać
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[RouletteGames]')
                    AND name = N'BetsJson'
                )
                BEGIN
                    ALTER TABLE [dbo].[RouletteGames] DROP COLUMN [BetsJson];
                END
            ");

            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'[dbo].[BaccaratGames]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [dbo].[BaccaratGames];
                END
            ");
        }
    }
}

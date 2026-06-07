-- ============================================================
-- SKRYPT TWORZACY BAZE DANYCH - Casino Royale
-- ============================================================
-- Wersja: 1.0
-- Silnik: SQL Server 2022+
-- ============================================================

-- ============================================================
-- 1. TWORZENIE BAZY DANYCH
-- ============================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CasinoDB')
BEGIN
    CREATE DATABASE [CasinoDB]
    ON PRIMARY (
        NAME = N'CasinoDB',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\CasinoDB.mdf',
        SIZE = 1024MB,
        FILEGROWTH = 256MB
    )
    LOG ON (
        NAME = N'CasinoDB_log',
        FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\CasinoDB_log.ldf',
        SIZE = 512MB,
        FILEGROWTH = 128MB
    );
END
GO

USE [CasinoDB];
GO

-- ============================================================
-- 2. KONTA NA BAZIE DANYCH (LOGINY + UZYTKOWNICY)
-- ============================================================

-- Konto dla administratora bazy (pelna kontrola)
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'CasinoAdmin')
BEGIN
    CREATE LOGIN [CasinoAdmin] WITH PASSWORD = N'CasinoAdmin_Str0ng!', CHECK_POLICY = ON;
END
GO

-- Konto dla aplikacji webowej (CRUD)
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'CasinoAppUser')
BEGIN
    CREATE LOGIN [CasinoAppUser] WITH PASSWORD = N'CasinoApp_P@ss123!', CHECK_POLICY = ON;
END
GO

-- Konto readonly dla raportow/analityki
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'CasinoReadOnly')
BEGIN
    CREATE LOGIN [CasinoReadOnly] WITH PASSWORD = N'CasinoRead_R3p0rt!', CHECK_POLICY = ON;
END
GO

-- Konto dev (pelna kontrola na dev, ograniczone na prod)
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'CasinoDev')
BEGIN
    CREATE LOGIN [CasinoDev] WITH PASSWORD = N'CasinoDev_D3v123!', CHECK_POLICY = ON;
END
GO

-- ============================================================
-- 3. TWORZENIE UZYTKOWNIKÓW BAZY DANYCH
-- ============================================================
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'CasinoAdmin')
    CREATE USER [CasinoAdmin] FOR LOGIN [CasinoAdmin];
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'CasinoAppUser')
    CREATE USER [CasinoAppUser] FOR LOGIN [CasinoAppUser];
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'CasinoReadOnly')
    CREATE USER [CasinoReadOnly] FOR LOGIN [CasinoReadOnly];
GO

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'CasinoDev')
    CREATE USER [CasinoDev] FOR LOGIN [CasinoDev];
GO

-- ============================================================
-- 4. ROLE SYSTEMOWE BAZY DANYCH
-- ============================================================

-- Rola: db_owner dla admina
EXEC sp_addrolemember N'db_owner', N'CasinoAdmin';
GO

-- Rola: db_datareader + db_datawriter dla aplikacji
EXEC sp_addrolemember N'db_datareader', N'CasinoAppUser';
EXEC sp_addrolemember N'db_datawriter', N'CasinoAppUser';
GO

-- Rola: db_datareader dla readonly
EXEC sp_addrolemember N'db_datareader', N'CasinoReadOnly';
GO

-- Rola: db_owner dla dev
EXEC sp_addrolemember N'db_owner', N'CasinoDev';
GO

-- ============================================================
-- 5. SCHEMATY (SCHAMAS)
-- ============================================================

-- Schema dla encji aplikacji
IF NOT EXISTS (SELECT name FROM sys.schemas WHERE name = N'app')
    EXEC('CREATE SCHEMA [app]');
GO

-- Schema dla security / auth
IF NOT EXISTS (SELECT name FROM sys.schemas WHERE name = N'security')
    EXEC('CREATE SCHEMA [security]');
GO

-- Schema dla raportow
IF NOT EXISTS (SELECT name FROM sys.schemas WHERE name = N'report')
    EXEC('CREATE SCHEMA [report]');
GO

-- Schema dla danych auditowych
IF NOT EXISTS (SELECT name FROM sys.schemas WHERE name = N'audit')
    EXEC('CREATE SCHEMA [audit]');
GO

-- Schema dla procedur
IF NOT EXISTS (SELECT name FROM sys.schemas WHERE name = N'prc')
    EXEC('CREATE SCHEMA [prc]');
GO

-- ============================================================
-- 6. PRZENIESIENIE DOMYSLNEGO SCHEMATU DLA UZYTKOWNIKOW
-- ============================================================
ALTER USER [CasinoAppUser] WITH DEFAULT_SCHEMA = [app];
ALTER USER [CasinoReadOnly] WITH DEFAULT_SCHEMA = [report];
ALTER USER [CasinoDev] WITH DEFAULT_SCHEMA = [app];
GO

-- ============================================================
-- 7. KONFIGURACJA BAZY DANYCH
-- ============================================================

-- Wlacz READ_COMMITTED_SNAPSHOT dla lepszej wspolbieznosci
ALTER DATABASE [CasinoDB] SET READ_COMMITTED_SNAPSHOT ON;
GO

-- Ustaw max degree of parallelism
ALTER DATABASE SCOPED CONFIGURATION SET MAXDOP = 4;
GO

-- Wlacz Parameter Sniffing
ALTER DATABASE SCOPED CONFIGURATION SET PARAMETER_SNIFFING = ON;
GO

PRINT 'Baza danych CasinoDB zostala utworzona i skonfigurowana pomyslnie.';
GO

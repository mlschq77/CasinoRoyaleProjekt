-- ============================================================
-- SKRYPT WDRUZENIOWY - Casino Royale (ALL IN ONE)
-- ============================================================
-- Uruchom w kolejnosci od gory do dolu
-- Wymaga: SQL Server 2022+, login z uprawnieniami sysadmin
-- ============================================================

PRINT '=== ROZPOCZECIE DEPLOYMENTU CASINO ROYALE DB ===';
GO

:setvar sqlpath "."

-- Krok 1: Baza danych, loginy, uzytkownicy, role, schematy
PRINT '--- Krok 1: Baza danych i security ---';
:r $(sqlpath)\01-create-database.sql

-- Krok 2: Tabele
PRINT '--- Krok 2: Tabele ---';
:r $(sqlpath)\02-create-tables.sql

-- Krok 3: Indeksy
PRINT '--- Krok 3: Indeksy ---';
:r $(sqlpath)\03-create-indexes.sql

-- Krok 4: Funkcje
PRINT '--- Krok 4: Funkcje ---';
:r $(sqlpath)\functions\fn_GetUserBalance.sql
:r $(sqlpath)\functions\fn_GetUserStats.sql
:r $(sqlpath)\functions\fn_GetActiveBonuses.sql

-- Krok 5: Procedury
PRINT '--- Krok 5: Procedury ---';
:r $(sqlpath)\procedures\prc_PlaceBet.sql
:r $(sqlpath)\procedures\prc_SettleBet.sql
:r $(sqlpath)\procedures\prc_ApplyBonusCode.sql
:r $(sqlpath)\procedures\prc_GetUserReport.sql

-- Krok 6: Triggery
PRINT '--- Krok 6: Triggery ---';
:r $(sqlpath)\triggers\trg_UpdateUserBalance.sql
:r $(sqlpath)\triggers\trg_LoginHistory_Audit.sql
:r $(sqlpath)\triggers\trg_Wallet_CheckBonusExpiry.sql

-- Krok 7: Security (audit, RLS, masking)
PRINT '--- Krok 7: Security ---';
:r $(sqlpath)\security\01-audit-tables.sql
:r $(sqlpath)\security\02-row-level-security.sql

-- Krok 8: Seed (dane startowe)
PRINT '--- Krok 8: Seed ---';
:r $(sqlpath)\seed\seed-bonus-codes.sql

PRINT '=== DEPLOYMENT ZAKONCZONY POMYSLNIE ===';
GO

-- ============================================================
-- Row-Level Security (RLS) - Casino Royale
-- ============================================================
USE [CasinoDB];
GO

-- ============================================================
-- Funkcja predykatu dla RLS na BetRecords
-- Uzytkownik widzi tylko swoje wlasne zaklady
-- Admin widzi wszystko
-- ============================================================
CREATE OR ALTER FUNCTION [security].[fn_BetRecordPredicate](@UserId INT)
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN SELECT 1 AS [AccessAllowed]
WHERE @UserId = CAST(SESSION_CONTEXT(N'UserId') AS INT)
   OR CAST(SESSION_CONTEXT(N'IsAdmin') AS BIT) = 1;
GO

-- ============================================================
-- Zastosowanie RLS na BetRecords (opcjonalne - wymaga SESSION_CONTEXT)
-- ============================================================
-- SECURITY POLICY [security].[BetRecordPolicy]
-- ON [app].[BetRecords]
-- FOR SELECT
-- WITH (STATE = ON)
-- AS (SELECT 1 AS [FilterPredicate] WHERE 1 = 1); -- odkomentuj gdy potrzebne
-- GO

-- ============================================================
-- Dynamic Data Masking - maskowanie danych wrażliwych
-- ============================================================

-- Maskuj Email w tabeli Users (widac tylko pierwszy znak)
ALTER TABLE [app].[Users]
    ALTER COLUMN [Email] ADD MASKED WITH (FUNCTION = 'email()');
GO

-- Maskuj HasloHash (cale haslo zakryte)
ALTER TABLE [app].[Users]
    ALTER COLUMN [HasloHash] ADD MASKED WITH (FUNCTION = 'default()');
GO

-- ============================================================
-- Przyznaj uprawnienia do odmaskowanych danych dla admina
-- ============================================================
GRANT UNMASK TO [CasinoAdmin];
GRANT UNMASK TO [CasinoDev];
GO

PRINT 'RLS i Data Masking skonfigurowane.';
GO

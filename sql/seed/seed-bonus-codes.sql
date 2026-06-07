-- ============================================================
-- SEED: Kody bonusowe
-- ============================================================
USE [CasinoDB];
GO

-- Wstaw domyslne kody bonusowe (jesli nie istnieja)
IF NOT EXISTS (SELECT 1 FROM [app].[KodyBonusowe] WHERE Kod = 'WITAJ')
    INSERT INTO [app].[KodyBonusowe] ([Kod], [MinimalnaWplata], [BonusProcentowy], [BonusKwotowy], [WageringMultiplier], [WaznyDo])
    VALUES ('WITAJ', 20, 100, 0, 35, DATEADD(YEAR, 1, SYSUTCDATETIME()));
GO

IF NOT EXISTS (SELECT 1 FROM [app].[KodyBonusowe] WHERE Kod = 'DEPOZYT50')
    INSERT INTO [app].[KodyBonusowe] ([Kod], [MinimalnaWplata], [BonusProcentowy], [BonusKwotowy], [WageringMultiplier], [WaznyDo])
    VALUES ('DEPOZYT50', 50, 50, 0, 25, DATEADD(YEAR, 1, SYSUTCDATETIME()));
GO

IF NOT EXISTS (SELECT 1 FROM [app].[KodyBonusowe] WHERE Kod = 'CASH100')
    INSERT INTO [app].[KodyBonusowe] ([Kod], [MinimalnaWplata], [BonusProcentowy], [BonusKwotowy], [WageringMultiplier], [WaznyDo])
    VALUES ('CASH100', 100, 0, 100, 20, DATEADD(YEAR, 1, SYSUTCDATETIME()));
GO

IF NOT EXISTS (SELECT 1 FROM [app].[KodyBonusowe] WHERE Kod = 'FREEBONUS')
    INSERT INTO [app].[KodyBonusowe] ([Kod], [MinimalnaWplata], [BonusProcentowy], [BonusKwotowy], [WageringMultiplier], [WaznyDo])
    VALUES ('FREEBONUS', 0, 0, 10, 40, DATEADD(MONTH, 3, SYSUTCDATETIME()));
GO

PRINT 'Kody bonusowe zostaly zasiane.';
GO

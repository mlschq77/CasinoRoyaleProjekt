-- ============================================================
-- FUNKCJA: fn.GetUserBalance
-- Opis: Zwraca calkowite saldo uzytkownika (real + bonus)
-- ============================================================
CREATE OR ALTER FUNCTION [fn].[GetUserBalance](@UserId INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Balance DECIMAL(18,2);

    SELECT @Balance = ISNULL(BalanceReal, 0) + ISNULL(BalanceBonus, 0)
    FROM [app].[Wallets]
    WHERE UserId = @UserId;

    RETURN ISNULL(@Balance, 0);
END;
GO

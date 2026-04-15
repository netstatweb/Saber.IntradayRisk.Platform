GO
CREATE PROCEDURE sp_MarketPulse
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Vol FLOAT = (SELECT SettingValue FROM MarketSettings WHERE SettingKey = 'Volatility');
    
    -- Symulujemy ruchy tylko na części rynku (realizm płynności)
    UPDATE TOP (5) PERCENT CurrentRisk
    SET Delta = Delta + ((RAND() - 0.5) * 0.05 * @Vol),
        Exposure = Exposure + ((RAND() - 0.5) * 1000 * @Vol),
        LastUpdate = SYSDATETIME();

    -- Zwracamy tylko to, co się zmieniło w ciągu ostatniej sekundy
    SELECT * FROM CurrentRisk WHERE LastUpdate >= DATEADD(SECOND, -1, SYSDATETIME());
END

GO
CREATE PROCEDURE sp_GenerateInitialData
AS
BEGIN
    SET NOCOUNT ON;
    TRUNCATE TABLE CurrentRisk;
    DECLARE @i INT = 1;
    WHILE @i <= 100000
    BEGIN
        INSERT INTO CurrentRisk (Ticker, Book, Currency, Delta, Gamma, Vega, Exposure)
        VALUES (
            'T-' + RIGHT('00000' + CAST(@i AS VARCHAR), 6),
            CASE WHEN @i%4=0 THEN 'FX_EMEA' WHEN @i%4=1 THEN 'FX_APAC' WHEN @i%4=2 THEN 'COMM_US' ELSE 'EQUITY' END,
            CASE WHEN @i%3=0 THEN 'USD' WHEN @i%3=1 THEN 'EUR' ELSE 'GBP' END,
            RAND()-0.5, RAND()*0.1, RAND()*0.5, RAND()*100000
        );
        SET @i = @i + 1;
    END
END
GO
EXEC sp_GenerateInitialData;
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

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- --------------------------------------------------------------------------------
-- Procedure: dbo.GetRiskMetricsPaged
-- Purpose:    Return a single page of risk rows and the total row count.
-- Notes:      This procedure returns TWO resultsets:
--               1) the page of rows (Ticker, Book, Currency, Delta, Gamma, Vega, Exposure)
--               2) a single-row resultset with TotalCount
-- --------------------------------------------------------------------------------
-- EN: Parameters
--   @PageNumber INT  - 1-based page index requested by the client.
--   @PageSize   INT  - Number of rows per page (must be > 0).
--   @Search     NVARCHAR(200) = NULL - Optional free-text fragment applied to multiple columns.
--
-- PL: Parametry
--   @PageNumber INT  - indeks strony 1-based żądany przez klienta.
--   @PageSize   INT  - liczba wierszy na stronę (musi być > 0).
--   @Search     NVARCHAR(200) = NULL - Opcjonalny fragment tekstowy dopasowywany do wielu kolumn.
--
-- EN: Implementation notes / guidance for callers
--  - The procedure uses parameterized queries to avoid SQL injection (do not concatenate SQL).
--  - Caller should execute reader and read first resultset (page rows), then call NextResult()
--    and read TotalCount from the second resultset.
--  - Using LIKE '%term%' is convenient but bypasses normal B-tree index seeks and may be slow
--    for large tables. For production workloads consider:
--      * Full-Text Search (CONTAINS) for text columns
--      * External search index (Elasticsearch) for fuzzy / substring search
--      * Denormalized search/view optimized for reads
--  - Converting numeric columns to NVARCHAR for text search is expensive. If you need numeric
--    filtering consider exposing typed filters (min/max) rather than string-matching numeric values.
--  - If you observe parameter sniffing issues, consider using OPTION (RECOMPILE) or local variables.
--
-- PL: Uwagi implementacyjne / wskazówki dla wywołującego
--  - Procedura używa parametrów (bez konkatenacji), co zapobiega SQL injection.
--  - Klient powinien odczytać najpierw wynik strony, wywołać NextResult() i odczytać TotalCount z drugiego resultsetu.
--  - LIKE '%term%' jest prosty, ale nie korzysta z indeksów B-tree i może być wolny przy dużych tabelach.
--    Rozważ w produkcji:
--      * Full-Text Search (CONTAINS) dla kolumn tekstowych
--      * Zewnętrzny indeks wyszukiwania (Elasticsearch) dla fuzzy/substring
--      * Denormalizowany widok zoptymalizowany pod odczyt
--  - Konwersja kolumn numerycznych do NVARCHAR jest kosztowna — dla filtrów liczbowych lepiej używać typowanych filtrów.
--  - Jeśli widzisz problemy z parameter sniffing, rozważ OPTION (RECOMPILE) lub użycie lokalnych zmiennych.
--
-- EN: Performance & Index suggestions
--  - Create non-clustered indexes on columns used in WHERE (Ticker, Book, Currency).
--  - Consider a composite index covering (Ticker, Book, Currency) if ORDER BY matches that prefix.
--  - For full-text scenarios create a full-text index on textual columns.
--
-- PL: Wydajność i sugestie indeksów
--  - Stwórz indeksy (non-clustered) na kolumnach używanych w WHERE (Ticker, Book, Currency).
--  - Rozważ indeks złożony (Ticker, Book, Currency) jeśli ORDER BY odpowiada prefiksowi indeksu.
--  - Dla full-text: utwórz indeks full-text na kolumnach tekstowych.
--
-- EN: Compatibility
--  - OFFSET...FETCH requires SQL Server 2012+. For older versions use ROW_NUMBER() based paging.
-- PL: Kompatybilność
--  - OFFSET...FETCH wymaga SQL Server 2012+. Dla starszych wersji użyj ROW_NUMBER() do paginacji.
--
-- EN: Security
--  - Grant EXECUTE on this procedure to the application role/user rather than granting broad table access.
-- PL: Bezpieczeństwo
--  - Nadaj uprawnienie EXECUTE na procedurę aplikacyjnemu użytkownikowi/zespole zamiast szerokiego dostępu do tabeli.
-- --------------------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.GetRiskMetricsPaged
    @PageNumber INT,
    @PageSize INT,
    @Search NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Page result
    SELECT Ticker, Book, Currency, Delta, Gamma, Vega, Exposure
    FROM dbo.CurrentRisk
    WHERE
        (@Search IS NULL OR @Search = '')
        OR (
            Ticker LIKE '%' + @Search + '%'
            OR Book LIKE '%' + @Search + '%'
            OR Currency LIKE '%' + @Search + '%'
            OR CONVERT(NVARCHAR(50), Delta) LIKE '%' + @Search + '%'
            OR CONVERT(NVARCHAR(50), Gamma) LIKE '%' + @Search + '%'
            OR CONVERT(NVARCHAR(50), Vega) LIKE '%' + @Search + '%'
            OR CONVERT(NVARCHAR(50), Exposure) LIKE '%' + @Search + '%'
        )
    ORDER BY Ticker, Book
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Total count
    SELECT COUNT(1) AS TotalCount
    FROM dbo.CurrentRisk
    WHERE
        (@Search IS NULL OR @Search = '')
        OR (
            Ticker LIKE '%' + @Search + '%'
            OR Book LIKE '%' + @Search + '%'
            OR Currency LIKE '%' + @Search + '%'
            OR CONVERT(NVARCHAR(50), Delta) LIKE '%' + @Search + '%'
            OR CONVERT(NVARCHAR(50), Gamma) LIKE '%' + @Search + '%'
            OR CONVERT(NVARCHAR(50), Vega) LIKE '%' + @Search + '%'
            OR CONVERT(NVARCHAR(50), Exposure) LIKE '%' + @Search + '%'
        );
END
GO
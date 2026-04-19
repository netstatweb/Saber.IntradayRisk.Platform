-- Tworzymy bazę danych (opcjonalnie, jeśli już masz to użyj swojej)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SaberRiskDB')
CREATE DATABASE SaberRiskDB;
GO
USE SaberRiskDB;
GO

-- Główna tabela pozycji
CREATE TABLE CurrentRisk (
    Ticker VARCHAR(20) PRIMARY KEY,
    Book VARCHAR(50),
    Currency VARCHAR(10),
    Delta FLOAT DEFAULT 0,
    Gamma FLOAT DEFAULT 0,
    Vega FLOAT DEFAULT 0,
    Exposure FLOAT DEFAULT 0,
    LastUpdate DATETIME2 DEFAULT SYSDATETIME()
);

-- Tabela sterująca silnikiem
CREATE TABLE MarketSettings (
    SettingKey VARCHAR(50) PRIMARY KEY,
    SettingValue FLOAT
);

INSERT INTO MarketSettings VALUES ('Volatility', 1.0), ('IsRunning', 1);
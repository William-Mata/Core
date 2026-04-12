/*
Ordem sugerida: 18
Objetivo: adicionar ContaDestinoId em despesa e receita para transferencias entre contas.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

SELECT
    t.name AS Tabela,
    c.name AS Coluna
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
WHERE t.name IN (N'Despesa', N'Receita')
  AND c.name = N'ContaDestinoId';
GO

IF COL_LENGTH(N'dbo.Despesa', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaDestinoId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD ContaDestinoId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_ContaBancaria_ContaDestinoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_ContaBancaria_ContaDestinoId
        FOREIGN KEY (ContaDestinoId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_ContaBancaria_ContaDestinoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_ContaBancaria_ContaDestinoId
        FOREIGN KEY (ContaDestinoId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ContaDestinoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ContaDestinoId ON dbo.Despesa (ContaDestinoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ContaDestinoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ContaDestinoId ON dbo.Receita (ContaDestinoId);
END;
GO

SELECT
    t.name AS Tabela,
    c.name AS Coluna
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
WHERE t.name IN (N'Despesa', N'Receita')
  AND c.name = N'ContaDestinoId';
GO

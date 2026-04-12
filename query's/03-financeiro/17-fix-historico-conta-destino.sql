/*
Ordem sugerida: 17
Objetivo: adicionar coluna ContaDestinoId na tabela de historico financeiro.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

SELECT
    t.name AS Tabela,
    c.name AS Coluna
FROM sys.tables t
LEFT JOIN sys.columns c
    ON c.object_id = t.object_id
   AND c.name = N'ContaDestinoId'
WHERE t.object_id = OBJECT_ID(N'dbo.HistoricoTransacaoFinanceira');
GO

IF OBJECT_ID(N'dbo.HistoricoTransacaoFinanceira', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD ContaDestinoId BIGINT NULL;
END;
GO

SELECT
    t.name AS Tabela,
    c.name AS Coluna
FROM sys.tables t
LEFT JOIN sys.columns c
    ON c.object_id = t.object_id
   AND c.name = N'ContaDestinoId'
WHERE t.object_id = OBJECT_ID(N'dbo.HistoricoTransacaoFinanceira');
GO

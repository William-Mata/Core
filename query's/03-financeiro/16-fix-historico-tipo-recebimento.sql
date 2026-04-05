/*
Ordem sugerida: 16
Objetivo: adicionar coluna TipoRecebimento na tabela de historico financeiro.
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
   AND c.name = N'TipoRecebimento'
WHERE t.object_id = OBJECT_ID(N'dbo.HistoricoTransacaoFinanceira');
GO

IF OBJECT_ID(N'dbo.HistoricoTransacaoFinanceira', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'TipoRecebimento') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD TipoRecebimento NVARCHAR(50) NULL;
END;
GO

SELECT
    t.name AS Tabela,
    c.name AS Coluna
FROM sys.tables t
LEFT JOIN sys.columns c
    ON c.object_id = t.object_id
   AND c.name = N'TipoRecebimento'
WHERE t.object_id = OBJECT_ID(N'dbo.HistoricoTransacaoFinanceira');
GO

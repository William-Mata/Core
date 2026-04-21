/*
Ordem sugerida: 24
Objetivo:
  - permitir persistencia de data/hora no estorno
  - ajustar colunas de estorno para DATETIME2(0)
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

/*
Pre-validacao dos tipos atuais
*/
SELECT
    t.name AS Tabela,
    c.name AS Coluna,
    ty.name AS TipoAtual,
    c.scale AS Escala,
    c.is_nullable AS AceitaNulo
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE (t.name = N'HistoricoTransacaoFinanceira' AND c.name = N'DataTransacao')
   OR (t.name = N'FaturaCartao' AND c.name = N'DataEstorno')
ORDER BY t.name, c.name;
GO

/*
1) Historico de transacao: date -> datetime2(0)
*/
IF COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'DataTransacao') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE t.name = N'HistoricoTransacaoFinanceira'
          AND SCHEMA_NAME(t.schema_id) = N'dbo'
          AND c.name = N'DataTransacao'
          AND NOT (ty.name = N'datetime2' AND c.scale = 0)
    )
    BEGIN
        ALTER TABLE dbo.HistoricoTransacaoFinanceira
            ALTER COLUMN DataTransacao DATETIME2(0) NOT NULL;
    END;
END;
GO

/*
2) Fatura de cartao: date -> datetime2(0)
*/
IF COL_LENGTH(N'dbo.FaturaCartao', N'DataEstorno') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.columns c
        INNER JOIN sys.tables t ON t.object_id = c.object_id
        INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
        WHERE t.name = N'FaturaCartao'
          AND SCHEMA_NAME(t.schema_id) = N'dbo'
          AND c.name = N'DataEstorno'
          AND NOT (ty.name = N'datetime2' AND c.scale = 0)
    )
    BEGIN
        ALTER TABLE dbo.FaturaCartao
            ALTER COLUMN DataEstorno DATETIME2(0) NULL;
    END;
END;
GO

/*
Pos-validacao dos tipos finais
*/
SELECT
    t.name AS Tabela,
    c.name AS Coluna,
    ty.name AS TipoFinal,
    c.scale AS Escala,
    c.is_nullable AS AceitaNulo
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE (t.name = N'HistoricoTransacaoFinanceira' AND c.name = N'DataTransacao')
   OR (t.name = N'FaturaCartao' AND c.name = N'DataEstorno')
ORDER BY t.name, c.name;
GO

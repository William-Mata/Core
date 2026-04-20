/*
Ordem sugerida: 23
Objetivo:
  - incluir suporte de persistencia para pagamento de fatura (DespesaPagamentoId)
  - permitir status Vencida na fatura de cartao
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

/*
Validacao previa
*/
SELECT
    t.name AS Tabela,
    c.name AS Coluna,
    ty.name AS Tipo,
    c.max_length,
    c.is_nullable
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
WHERE t.name = N'FaturaCartao'
  AND c.name IN (N'DespesaPagamentoId', N'Status');
GO

SELECT
    cc.name AS CheckConstraint,
    cc.definition
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.FaturaCartao')
  AND cc.name = N'CK_FaturaCartao_Status';
GO

/*
1) Coluna de vinculacao da despesa de pagamento da fatura
*/
IF COL_LENGTH(N'dbo.FaturaCartao', N'DespesaPagamentoId') IS NULL
BEGIN
    ALTER TABLE dbo.FaturaCartao
        ADD DespesaPagamentoId BIGINT NULL;
END;
GO

/*
2) FK para despesa de pagamento
*/
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FaturaCartao_Despesa_DespesaPagamentoId')
BEGIN
    ALTER TABLE dbo.FaturaCartao
        WITH CHECK ADD CONSTRAINT FK_FaturaCartao_Despesa_DespesaPagamentoId
        FOREIGN KEY (DespesaPagamentoId) REFERENCES dbo.Despesa (Id);
END;
GO

/*
3) Indice da coluna de despesa de pagamento
*/
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FaturaCartao_DespesaPagamentoId' AND object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_FaturaCartao_DespesaPagamentoId
        ON dbo.FaturaCartao (DespesaPagamentoId);
END;
GO

/*
4) Ajuste do CHECK de status para incluir Vencida
*/
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_FaturaCartao_Status' AND parent_object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    ALTER TABLE dbo.FaturaCartao DROP CONSTRAINT CK_FaturaCartao_Status;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_FaturaCartao_Status' AND parent_object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    ALTER TABLE dbo.FaturaCartao
        WITH CHECK ADD CONSTRAINT CK_FaturaCartao_Status
        CHECK (Status IN (N'Aberta', N'Fechada', N'Efetivada', N'Estornada', N'Vencida'));
END;
GO

/*
Validacao final
*/
SELECT
    t.name AS Tabela,
    c.name AS Coluna
FROM sys.columns c
INNER JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.name = N'FaturaCartao'
  AND c.name = N'DespesaPagamentoId';
GO

SELECT
    cc.name AS CheckConstraint,
    cc.definition
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.FaturaCartao')
  AND cc.name = N'CK_FaturaCartao_Status';
GO

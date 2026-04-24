/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: FaturaCartao
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 20-fatura-cartao.sql (bloco 2) */
IF OBJECT_ID(N'dbo.FaturaCartao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FaturaCartao
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_FaturaCartao_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        CartaoId BIGINT NOT NULL,
        Competencia CHAR(7) NOT NULL,
        DataVencimento DATE NULL,
        DataFechamento DATE NULL,
        DataEfetivacao DATE NULL,
        DataEstorno DATE NULL,
        ValorTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_FaturaCartao_ValorTotal DEFAULT (0),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_FaturaCartao_Status DEFAULT (N'Aberta'),
        CONSTRAINT PK_FaturaCartao PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_FaturaCartao_Status CHECK (Status IN (N'Aberta', N'Fechada', N'Efetivada', N'Estornada'))
    );
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 3) */
IF COL_LENGTH(N'dbo.FaturaCartao', N'DataVencimento') IS NULL
BEGIN
    ALTER TABLE dbo.FaturaCartao ADD DataVencimento DATE NULL;
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 8) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FaturaCartao_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.FaturaCartao
        WITH CHECK ADD CONSTRAINT FK_FaturaCartao_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 9) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FaturaCartao_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.FaturaCartao
        WITH CHECK ADD CONSTRAINT FK_FaturaCartao_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 14) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FaturaCartao_UsuarioCadastroId_CartaoId_Competencia' AND object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_FaturaCartao_UsuarioCadastroId_CartaoId_Competencia
        ON dbo.FaturaCartao (UsuarioCadastroId, CartaoId, Competencia);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 15) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FaturaCartao_CartaoId_Competencia' AND object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_FaturaCartao_CartaoId_Competencia
        ON dbo.FaturaCartao (CartaoId, Competencia);
END;
GO

/* Origem: 23-fix-fatura-cartao-efetivacao-estorno.sql (bloco 4) */
/*
1) Coluna de vinculacao da despesa de pagamento da fatura
*/
IF COL_LENGTH(N'dbo.FaturaCartao', N'DespesaPagamentoId') IS NULL
BEGIN
    ALTER TABLE dbo.FaturaCartao
        ADD DespesaPagamentoId BIGINT NULL;
END;
GO

/* Origem: 23-fix-fatura-cartao-efetivacao-estorno.sql (bloco 5) */
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

/* Origem: 23-fix-fatura-cartao-efetivacao-estorno.sql (bloco 6) */
/*
3) Indice da coluna de despesa de pagamento
*/
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FaturaCartao_DespesaPagamentoId' AND object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_FaturaCartao_DespesaPagamentoId
        ON dbo.FaturaCartao (DespesaPagamentoId);
END;
GO

/* Origem: 23-fix-fatura-cartao-efetivacao-estorno.sql (bloco 7) */
/*
4) Ajuste do CHECK de status para incluir Vencida
*/
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_FaturaCartao_Status' AND parent_object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    ALTER TABLE dbo.FaturaCartao DROP CONSTRAINT CK_FaturaCartao_Status;
END;
GO

/* Origem: 23-fix-fatura-cartao-efetivacao-estorno.sql (bloco 8) */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_FaturaCartao_Status' AND parent_object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    ALTER TABLE dbo.FaturaCartao
        WITH CHECK ADD CONSTRAINT CK_FaturaCartao_Status
        CHECK (Status IN (N'Aberta', N'Fechada', N'Efetivada', N'Estornada', N'Vencida'));
END;
GO

/* Origem: 24-fix-estorno-datetime2.sql (bloco 4) */
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



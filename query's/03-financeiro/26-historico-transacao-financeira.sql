/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: HistoricoTransacaoFinanceira
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 08-historico-transacao-financeira.sql (bloco 1) */
IF OBJECT_ID('dbo.HistoricoTransacaoFinanceira', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistoricoTransacaoFinanceira
    (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DataHoraCadastro DATETIME2 NOT NULL CONSTRAINT DF_HistoricoTransacaoFinanceira_DataHoraCadastro DEFAULT SYSUTCDATETIME(),
        UsuarioOperacaoId INT NOT NULL,
        TipoTransacao NVARCHAR(30) NOT NULL,
        TransacaoId BIGINT NOT NULL,
        TipoOperacao NVARCHAR(30) NOT NULL,
        TipoConta NVARCHAR(30) NOT NULL,
        ContaBancariaId BIGINT NULL,
        ContaDestinoId BIGINT NULL,
        CartaoId BIGINT NULL,
        DataTransacao DATE NOT NULL,
        Descricao NVARCHAR(300) NOT NULL,
        Observacao NVARCHAR(500) NULL,
        OcultarDoHistorico BIT NOT NULL CONSTRAINT DF_HistoricoTransacaoFinanceira_OcultarDoHistorico DEFAULT(0),
        TipoPagamento NVARCHAR(50) NULL,
        TipoRecebimento NVARCHAR(50) NULL,
        ValorAntesTransacao DECIMAL(18,2) NOT NULL,
        ValorTransacao DECIMAL(18,2) NOT NULL,
        ValorDepoisTransacao DECIMAL(18,2) NOT NULL,
        CONSTRAINT CK_HistoricoTransacaoFinanceira_ContaOuCartaoExclusivo
            CHECK (NOT (ContaBancariaId IS NOT NULL AND CartaoId IS NOT NULL))
    );
END;
GO

/* Origem: 08-historico-transacao-financeira.sql (bloco 2) */
IF COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD ContaDestinoId BIGINT NULL;
END;
GO

/* Origem: 08-historico-transacao-financeira.sql (bloco 3) */
IF COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'TipoRecebimento') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD TipoRecebimento NVARCHAR(50) NULL;
END;
GO

/* Origem: 08-historico-transacao-financeira.sql (bloco 4) */
IF COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'Observacao') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD Observacao NVARCHAR(500) NULL;
END;
GO

/* Origem: 08-historico-transacao-financeira.sql (bloco 5) */
IF COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'OcultarDoHistorico') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD OcultarDoHistorico BIT NOT NULL
            CONSTRAINT DF_HistoricoTransacaoFinanceira_OcultarDoHistorico DEFAULT(0);
END;
GO

/* Origem: 16-fix-historico-tipo-recebimento.sql (bloco 3) */
IF OBJECT_ID(N'dbo.HistoricoTransacaoFinanceira', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'TipoRecebimento') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD TipoRecebimento NVARCHAR(50) NULL;
END;
GO

/* Origem: 17-fix-historico-conta-destino.sql (bloco 3) */
IF OBJECT_ID(N'dbo.HistoricoTransacaoFinanceira', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'ContaDestinoId') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD ContaDestinoId BIGINT NULL;
END;
GO

/* Origem: 24-fix-estorno-datetime2.sql (bloco 3) */
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



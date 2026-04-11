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

IF COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'TipoRecebimento') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD TipoRecebimento NVARCHAR(50) NULL;
END;
GO

IF COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'Observacao') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD Observacao NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH(N'dbo.HistoricoTransacaoFinanceira', N'OcultarDoHistorico') IS NULL
BEGIN
    ALTER TABLE dbo.HistoricoTransacaoFinanceira
        ADD OcultarDoHistorico BIT NOT NULL
            CONSTRAINT DF_HistoricoTransacaoFinanceira_OcultarDoHistorico DEFAULT(0);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_HistoricoTransacaoFinanceira_TipoTransacao_TransacaoId_DataHoraCadastro'
      AND object_id = OBJECT_ID('dbo.HistoricoTransacaoFinanceira')
)
BEGIN
    CREATE INDEX IX_HistoricoTransacaoFinanceira_TipoTransacao_TransacaoId_DataHoraCadastro
        ON dbo.HistoricoTransacaoFinanceira (TipoTransacao, TransacaoId, DataHoraCadastro DESC);
END;
GO

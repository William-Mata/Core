/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: Reembolso
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 07-reembolso.sql (bloco 1) */
/*
Reembolso
*/

IF OBJECT_ID(N'dbo.Reembolso', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reembolso
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reembolso PRIMARY KEY,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Reembolso_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Solicitante NVARCHAR(150) NOT NULL,
        Competencia CHAR(7) NOT NULL CONSTRAINT DF_Reembolso_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120)),
        DataLancamento DATETIME2(0) NOT NULL,
        DataVencimento DATE NULL,
        DataEfetivacao DATETIME2(0) NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        CONSTRAINT CK_Reembolso_ValorTotal CHECK (ValorTotal >= 0),
        CONSTRAINT CK_Reembolso_Status CHECK (Status IN (N'Aguardando', N'Aprovado', N'Pago', N'Cancelado', N'Rejeitado'))
    );
END;
GO

/* Origem: 07-reembolso.sql (bloco 2) */
IF COL_LENGTH(N'dbo.Reembolso', N'DataVencimento') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso
        ADD DataVencimento DATE NULL;
END;
GO

/* Origem: 07-reembolso.sql (bloco 3) */
UPDATE dbo.Reembolso
SET DataVencimento = DataLancamento
WHERE DataVencimento IS NULL;
GO

/* Origem: 07-reembolso.sql (bloco 4) */
IF COL_LENGTH(N'dbo.Reembolso', N'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso
        ADD DataEfetivacao DATETIME2(0) NULL;
END;
GO

/* Origem: 07-reembolso.sql (bloco 5) */
IF COL_LENGTH(N'dbo.Reembolso', N'Competencia') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso
        ADD Competencia CHAR(7) NOT NULL
            CONSTRAINT DF_Reembolso_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120));
END;
GO

/* Origem: 07-reembolso.sql (bloco 6) */
UPDATE dbo.Reembolso
SET Competencia = CONVERT(char(7), DataLancamento, 120)
WHERE Competencia IS NULL OR Competencia = '';
GO

/* Origem: 07-reembolso.sql (bloco 7) */
IF COL_LENGTH(N'dbo.Reembolso', N'DataLancamento') IS NULL
BEGIN
    IF COL_LENGTH(N'dbo.Reembolso', N'DataSolicitacao') IS NOT NULL
    BEGIN
        EXEC sp_rename N'dbo.Reembolso.DataSolicitacao', N'DataLancamento', N'COLUMN';
    END
    ELSE
    BEGIN
        ALTER TABLE dbo.Reembolso
            ADD DataLancamento DATETIME2(0) NOT NULL
                CONSTRAINT DF_Reembolso_DataLancamento DEFAULT (CONVERT(datetime2(0), SYSUTCDATETIME()));
    END
END;
GO

/* Origem: 07-reembolso.sql (bloco 8) */
IF COL_LENGTH(N'dbo.Reembolso', N'DataLancamento') IS NOT NULL
   AND COL_LENGTH(N'dbo.Reembolso', N'DataSolicitacao') IS NOT NULL
BEGIN
    EXEC(N'UPDATE dbo.Reembolso
          SET DataLancamento = ISNULL(DataLancamento, DataSolicitacao)
          WHERE DataSolicitacao IS NOT NULL;');

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_DataSolicitacao' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
    BEGIN
        DROP INDEX IX_Reembolso_DataSolicitacao ON dbo.Reembolso;
    END

    ALTER TABLE dbo.Reembolso
        DROP COLUMN DataSolicitacao;
END;
GO

/* Origem: 07-reembolso.sql (bloco 9) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Reembolso_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Reembolso
        WITH CHECK ADD CONSTRAINT FK_Reembolso_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 07-reembolso.sql (bloco 10) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_DataLancamento' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_DataLancamento
        ON dbo.Reembolso (DataLancamento DESC, Id DESC);
END;
GO

/* Origem: 07-reembolso.sql (bloco 11) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_Competencia' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_Competencia
        ON dbo.Reembolso (Competencia, Id DESC);
END;
GO

/* Origem: 09-fix-colunas-efetivacao-recorrencia.sql (bloco 22) */
IF COL_LENGTH(N'dbo.Reembolso', N'DataEfetivacao') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso ADD DataEfetivacao DATETIME2(0) NULL;
END;
GO

/* Origem: 11-fix-reembolso-datalancamento.sql (bloco 2) */
IF COL_LENGTH(N'dbo.Reembolso', N'DataLancamento') IS NULL
BEGIN
    IF COL_LENGTH(N'dbo.Reembolso', N'DataSolicitacao') IS NOT NULL
    BEGIN
        EXEC sp_rename N'dbo.Reembolso.DataSolicitacao', N'DataLancamento', N'COLUMN';
    END
    ELSE
    BEGIN
        ALTER TABLE dbo.Reembolso
            ADD DataLancamento DATETIME2(0) NOT NULL
                CONSTRAINT DF_Reembolso_DataLancamento DEFAULT (CONVERT(datetime2(0), SYSUTCDATETIME()));
    END
END;
GO

/* Origem: 11-fix-reembolso-datalancamento.sql (bloco 3) */
IF COL_LENGTH(N'dbo.Reembolso', N'Competencia') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso
        ADD Competencia CHAR(7) NOT NULL
            CONSTRAINT DF_Reembolso_Competencia DEFAULT (CONVERT(char(7), SYSUTCDATETIME(), 120));
END;
GO

/* Origem: 11-fix-reembolso-datalancamento.sql (bloco 4) */
UPDATE dbo.Reembolso
SET Competencia = CONVERT(char(7), DataLancamento, 120)
WHERE Competencia IS NULL OR Competencia = '';
GO

/* Origem: 11-fix-reembolso-datalancamento.sql (bloco 5) */
IF COL_LENGTH(N'dbo.Reembolso', N'DataLancamento') IS NOT NULL
   AND COL_LENGTH(N'dbo.Reembolso', N'DataSolicitacao') IS NOT NULL
BEGIN
    EXEC(N'UPDATE dbo.Reembolso
          SET DataLancamento = ISNULL(DataLancamento, DataSolicitacao)
          WHERE DataSolicitacao IS NOT NULL;');

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_DataSolicitacao' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
    BEGIN
        DROP INDEX IX_Reembolso_DataSolicitacao ON dbo.Reembolso;
    END

    ALTER TABLE dbo.Reembolso
        DROP COLUMN DataSolicitacao;
END;
GO

/* Origem: 11-fix-reembolso-datalancamento.sql (bloco 6) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_DataLancamento' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_DataLancamento
        ON dbo.Reembolso (DataLancamento DESC, Id DESC);
END;
GO

/* Origem: 11-fix-reembolso-datalancamento.sql (bloco 7) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_Competencia' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_Competencia
        ON dbo.Reembolso (Competencia, Id DESC);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 6) */
IF COL_LENGTH(N'dbo.Reembolso', N'CartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso ADD CartaoId BIGINT NULL;
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 7) */
IF COL_LENGTH(N'dbo.Reembolso', N'FaturaCartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso ADD FaturaCartaoId BIGINT NULL;
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 12) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Reembolso_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.Reembolso
        WITH CHECK ADD CONSTRAINT FK_Reembolso_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 13) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Reembolso_FaturaCartao_FaturaCartaoId')
BEGIN
    ALTER TABLE dbo.Reembolso
        WITH CHECK ADD CONSTRAINT FK_Reembolso_FaturaCartao_FaturaCartaoId
        FOREIGN KEY (FaturaCartaoId) REFERENCES dbo.FaturaCartao (Id);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 18) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_CartaoId' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_CartaoId
        ON dbo.Reembolso (CartaoId);
END;
GO

/* Origem: 20-fatura-cartao.sql (bloco 19) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_FaturaCartaoId' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_FaturaCartaoId
        ON dbo.Reembolso (FaturaCartaoId);
END;
GO



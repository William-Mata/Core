/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: DespesaAmigoRateio
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 05-despesa.sql (bloco 28) */
IF OBJECT_ID(N'dbo.DespesaAmigoRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaAmigoRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaAmigoRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        AmigoNome NVARCHAR(150) NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_DespesaAmigoRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 05-despesa.sql (bloco 29) */
IF COL_LENGTH('dbo.DespesaAmigoRateio', 'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

/* Origem: 05-despesa.sql (bloco 30) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 05-despesa.sql (bloco 31) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 05-despesa.sql (bloco 32) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAmigoRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAmigoRateio_DespesaId
        ON dbo.DespesaAmigoRateio (DespesaId);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 4) */
IF OBJECT_ID(N'dbo.DespesaAmigoRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaAmigoRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaAmigoRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        AmigoNome NVARCHAR(150) NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_DespesaAmigoRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 6) */
IF COL_LENGTH(N'dbo.DespesaAmigoRateio', N'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 18) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 19) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 26) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAmigoRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAmigoRateio_DespesaId
        ON dbo.DespesaAmigoRateio (DespesaId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 31) */
IF COL_LENGTH(N'dbo.DespesaAmigoRateio', N'AmigoId') IS NULL
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio ADD AmigoId INT NULL;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 33) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAmigoRateio_AmigoId' AND object_id = OBJECT_ID(N'dbo.DespesaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAmigoRateio_AmigoId
        ON dbo.DespesaAmigoRateio (AmigoId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 35) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Usuario_AmigoId')
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.DespesaAmigoRateio r
       LEFT JOIN dbo.Usuario u ON u.Id = r.AmigoId
       WHERE r.AmigoId IS NOT NULL
         AND u.Id IS NULL
   )
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Usuario_AmigoId
        FOREIGN KEY (AmigoId) REFERENCES dbo.Usuario (Id);
END;
GO



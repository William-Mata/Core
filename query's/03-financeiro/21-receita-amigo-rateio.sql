/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: ReceitaAmigoRateio
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 06-receita.sql (bloco 28) */
IF OBJECT_ID(N'dbo.ReceitaAmigoRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceitaAmigoRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReceitaAmigoRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        AmigoNome NVARCHAR(150) NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_ReceitaAmigoRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 06-receita.sql (bloco 29) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 06-receita.sql (bloco 30) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 06-receita.sql (bloco 31) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAmigoRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAmigoRateio_ReceitaId
        ON dbo.ReceitaAmigoRateio (ReceitaId);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 5) */
IF OBJECT_ID(N'dbo.ReceitaAmigoRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceitaAmigoRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReceitaAmigoRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        AmigoNome NVARCHAR(150) NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_ReceitaAmigoRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 7) */
IF COL_LENGTH(N'dbo.ReceitaAmigoRateio', N'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 20) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 21) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 27) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAmigoRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAmigoRateio_ReceitaId
        ON dbo.ReceitaAmigoRateio (ReceitaId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 32) */
IF COL_LENGTH(N'dbo.ReceitaAmigoRateio', N'AmigoId') IS NULL
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio ADD AmigoId INT NULL;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 34) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAmigoRateio_AmigoId' AND object_id = OBJECT_ID(N'dbo.ReceitaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAmigoRateio_AmigoId
        ON dbo.ReceitaAmigoRateio (AmigoId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 36) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Usuario_AmigoId')
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.ReceitaAmigoRateio r
       LEFT JOIN dbo.Usuario u ON u.Id = r.AmigoId
       WHERE r.AmigoId IS NOT NULL
         AND u.Id IS NULL
   )
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Usuario_AmigoId
        FOREIGN KEY (AmigoId) REFERENCES dbo.Usuario (Id);
END;
GO



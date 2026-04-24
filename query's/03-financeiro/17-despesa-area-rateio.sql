/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: DespesaAreaRateio
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 05-despesa.sql (bloco 33) */
IF OBJECT_ID(N'dbo.DespesaAreaRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaAreaRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaAreaRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        AreaId BIGINT NOT NULL,
        SubAreaId BIGINT NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_DespesaAreaRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 05-despesa.sql (bloco 34) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 05-despesa.sql (bloco 35) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Area_AreaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

/* Origem: 05-despesa.sql (bloco 36) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_SubArea_SubAreaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_SubArea_SubAreaId
        FOREIGN KEY (SubAreaId) REFERENCES dbo.SubArea (Id);
END;
GO

/* Origem: 05-despesa.sql (bloco 37) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 05-despesa.sql (bloco 38) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAreaRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAreaRateio_DespesaId
        ON dbo.DespesaAreaRateio (DespesaId);
END;
GO

/* Origem: 05-despesa.sql (bloco 39) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAreaRateio_AreaId_SubAreaId' AND object_id = OBJECT_ID(N'dbo.DespesaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAreaRateio_AreaId_SubAreaId
        ON dbo.DespesaAreaRateio (AreaId, SubAreaId);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 2) */
IF OBJECT_ID(N'dbo.DespesaAreaRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaAreaRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaAreaRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        AreaId BIGINT NOT NULL,
        SubAreaId BIGINT NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_DespesaAreaRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 8) */
IF COL_LENGTH(N'dbo.DespesaAreaRateio', N'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 10) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 11) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Area_AreaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 12) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_SubArea_SubAreaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_SubArea_SubAreaId
        FOREIGN KEY (SubAreaId) REFERENCES dbo.SubArea (Id);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 13) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 22) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAreaRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAreaRateio_DespesaId
        ON dbo.DespesaAreaRateio (DespesaId);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 23) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAreaRateio_AreaId_SubAreaId' AND object_id = OBJECT_ID(N'dbo.DespesaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAreaRateio_AreaId_SubAreaId
        ON dbo.DespesaAreaRateio (AreaId, SubAreaId);
END;
GO



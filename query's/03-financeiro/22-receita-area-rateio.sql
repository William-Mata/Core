/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: ReceitaAreaRateio
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 06-receita.sql (bloco 32) */
IF OBJECT_ID(N'dbo.ReceitaAreaRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceitaAreaRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReceitaAreaRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        AreaId BIGINT NOT NULL,
        SubAreaId BIGINT NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_ReceitaAreaRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 06-receita.sql (bloco 33) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 06-receita.sql (bloco 34) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Area_AreaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

/* Origem: 06-receita.sql (bloco 35) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_SubArea_SubAreaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_SubArea_SubAreaId
        FOREIGN KEY (SubAreaId) REFERENCES dbo.SubArea (Id);
END;
GO

/* Origem: 06-receita.sql (bloco 36) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 06-receita.sql (bloco 37) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAreaRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAreaRateio_ReceitaId
        ON dbo.ReceitaAreaRateio (ReceitaId);
END;
GO

/* Origem: 06-receita.sql (bloco 38) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAreaRateio_AreaId_SubAreaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAreaRateio_AreaId_SubAreaId
        ON dbo.ReceitaAreaRateio (AreaId, SubAreaId);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 3) */
IF OBJECT_ID(N'dbo.ReceitaAreaRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceitaAreaRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReceitaAreaRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        AreaId BIGINT NOT NULL,
        SubAreaId BIGINT NOT NULL,
        Valor DECIMAL(18,2) NULL,
        CONSTRAINT PK_ReceitaAreaRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 9) */
IF COL_LENGTH(N'dbo.ReceitaAreaRateio', N'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 14) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 15) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Area_AreaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 16) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_SubArea_SubAreaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_SubArea_SubAreaId
        FOREIGN KEY (SubAreaId) REFERENCES dbo.SubArea (Id);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 17) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 24) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAreaRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAreaRateio_ReceitaId
        ON dbo.ReceitaAreaRateio (ReceitaId);
END;
GO

/* Origem: 10-fix-rateios-ausentes.sql (bloco 25) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAreaRateio_AreaId_SubAreaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAreaRateio_AreaId_SubAreaId
        ON dbo.ReceitaAreaRateio (AreaId, SubAreaId);
END;
GO



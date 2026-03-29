/*
Ordem sugerida: 10
Objetivo: corrigir bases antigas com tabelas de rateio ausentes no modulo financeiro.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

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

IF COL_LENGTH(N'dbo.DespesaAmigoRateio', N'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

IF COL_LENGTH(N'dbo.ReceitaAmigoRateio', N'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

IF COL_LENGTH(N'dbo.DespesaAreaRateio', N'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

IF COL_LENGTH(N'dbo.ReceitaAreaRateio', N'Valor') IS NULL
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio ADD Valor DECIMAL(18,2) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Area_AreaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_SubArea_SubAreaId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_SubArea_SubAreaId
        FOREIGN KEY (SubAreaId) REFERENCES dbo.SubArea (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAreaRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAreaRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Area_AreaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_SubArea_SubAreaId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_SubArea_SubAreaId
        FOREIGN KEY (SubAreaId) REFERENCES dbo.SubArea (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAreaRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaAreaRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAreaRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAreaRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAreaRateio_DespesaId
        ON dbo.DespesaAreaRateio (DespesaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAreaRateio_AreaId_SubAreaId' AND object_id = OBJECT_ID(N'dbo.DespesaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAreaRateio_AreaId_SubAreaId
        ON dbo.DespesaAreaRateio (AreaId, SubAreaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAreaRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAreaRateio_ReceitaId
        ON dbo.ReceitaAreaRateio (ReceitaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAreaRateio_AreaId_SubAreaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAreaRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAreaRateio_AreaId_SubAreaId
        ON dbo.ReceitaAreaRateio (AreaId, SubAreaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAmigoRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAmigoRateio_DespesaId
        ON dbo.DespesaAmigoRateio (DespesaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAmigoRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAmigoRateio_ReceitaId
        ON dbo.ReceitaAmigoRateio (ReceitaId);
END;
GO

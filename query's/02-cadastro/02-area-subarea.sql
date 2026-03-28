/*
Ordem sugerida: 02
Objetivo: criar objetos de cadastro de area e subarea.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

IF OBJECT_ID(N'dbo.Area', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Area
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Area_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Nome NVARCHAR(150) NOT NULL,
        Tipo NVARCHAR(20) NOT NULL CONSTRAINT DF_Area_Tipo DEFAULT (N'Despesa'),
        CONSTRAINT CK_Area_Tipo CHECK (Tipo IN (N'Despesa', N'Receita')),
        CONSTRAINT PK_Area PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF COL_LENGTH(N'dbo.Area', N'Tipo') IS NULL
BEGIN
    ALTER TABLE dbo.Area
        ADD Tipo NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Area_Tipo DEFAULT (N'Despesa');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Area_Tipo' AND parent_object_id = OBJECT_ID(N'dbo.Area'))
BEGIN
    ALTER TABLE dbo.Area
        WITH CHECK ADD CONSTRAINT CK_Area_Tipo CHECK (Tipo IN (N'Despesa', N'Receita'));
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Area_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Area
        WITH CHECK ADD CONSTRAINT FK_Area_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Area_UsuarioCadastroId_Nome' AND object_id = OBJECT_ID(N'dbo.Area'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Area_UsuarioCadastroId_Nome
        ON dbo.Area (UsuarioCadastroId, Nome);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Area_Tipo_Nome' AND object_id = OBJECT_ID(N'dbo.Area'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Area_Tipo_Nome
        ON dbo.Area (Tipo, Nome);
END;
GO

IF OBJECT_ID(N'dbo.SubArea', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubArea
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_SubArea_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        AreaId BIGINT NOT NULL,
        Nome NVARCHAR(150) NOT NULL,
        CONSTRAINT PK_SubArea PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SubArea_Area_AreaId')
BEGIN
    ALTER TABLE dbo.SubArea
        WITH CHECK ADD CONSTRAINT FK_SubArea_Area_AreaId
        FOREIGN KEY (AreaId) REFERENCES dbo.Area (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_SubArea_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.SubArea
        WITH CHECK ADD CONSTRAINT FK_SubArea_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SubArea_AreaId_Nome' AND object_id = OBJECT_ID(N'dbo.SubArea'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SubArea_AreaId_Nome
        ON dbo.SubArea (AreaId, Nome);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SubArea_UsuarioCadastroId' AND object_id = OBJECT_ID(N'dbo.SubArea'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SubArea_UsuarioCadastroId
        ON dbo.SubArea (UsuarioCadastroId);
END;
GO

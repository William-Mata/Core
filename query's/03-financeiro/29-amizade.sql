/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: Amizade
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 9) */
IF OBJECT_ID(N'dbo.Amizade', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Amizade
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Amizade_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        UsuarioAId INT NOT NULL,
        UsuarioBId INT NOT NULL,
        CONSTRAINT PK_Amizade PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Amizade_ParOrdenado CHECK (UsuarioAId < UsuarioBId)
    );
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 10) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Amizade_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Amizade
        WITH CHECK ADD CONSTRAINT FK_Amizade_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 11) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Amizade_Usuario_UsuarioAId')
BEGIN
    ALTER TABLE dbo.Amizade
        WITH CHECK ADD CONSTRAINT FK_Amizade_Usuario_UsuarioAId
        FOREIGN KEY (UsuarioAId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 12) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Amizade_Usuario_UsuarioBId')
BEGIN
    ALTER TABLE dbo.Amizade
        WITH CHECK ADD CONSTRAINT FK_Amizade_Usuario_UsuarioBId
        FOREIGN KEY (UsuarioBId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 13) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Amizade_UsuarioAId_UsuarioBId' AND object_id = OBJECT_ID(N'dbo.Amizade'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Amizade_UsuarioAId_UsuarioBId
        ON dbo.Amizade (UsuarioAId, UsuarioBId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 14) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Amizade_UsuarioAId' AND object_id = OBJECT_ID(N'dbo.Amizade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Amizade_UsuarioAId
        ON dbo.Amizade (UsuarioAId);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 15) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Amizade_UsuarioBId' AND object_id = OBJECT_ID(N'dbo.Amizade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Amizade_UsuarioBId
        ON dbo.Amizade (UsuarioBId);
END;
GO



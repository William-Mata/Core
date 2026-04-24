/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: RefreshToken */

IF OBJECT_ID(N'dbo.RefreshToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshToken
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_RefreshToken_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        UsuarioId INT NOT NULL,
        Token NVARCHAR(128) NOT NULL,
        ExpiraEmUtc DATETIME2(0) NOT NULL,
        RevogadoEmUtc DATETIME2(0) NULL,
        CONSTRAINT PK_RefreshToken PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_RefreshToken_Token UNIQUE (Token)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RefreshToken_Usuario_UsuarioId')
BEGIN
    ALTER TABLE dbo.RefreshToken
        WITH CHECK ADD CONSTRAINT FK_RefreshToken_Usuario_UsuarioId
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuario (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RefreshToken_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.RefreshToken
        WITH CHECK ADD CONSTRAINT FK_RefreshToken_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshToken_UsuarioId_ExpiraEmUtc' AND object_id = OBJECT_ID(N'dbo.RefreshToken'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RefreshToken_UsuarioId_ExpiraEmUtc
        ON dbo.RefreshToken (UsuarioId, ExpiraEmUtc);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshToken_UsuarioCadastroId' AND object_id = OBJECT_ID(N'dbo.RefreshToken'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RefreshToken_UsuarioCadastroId
        ON dbo.RefreshToken (UsuarioCadastroId);
END;
GO




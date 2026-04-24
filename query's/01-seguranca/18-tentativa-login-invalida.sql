/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: TentativaLoginInvalida */

IF OBJECT_ID(N'dbo.TentativaLoginInvalida', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TentativaLoginInvalida
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_TentativaLoginInvalida_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL CONSTRAINT DF_TentativaLoginInvalida_UsuarioCadastroId DEFAULT (1),
        Email NVARCHAR(200) NOT NULL,
        TentativasInvalidas INT NOT NULL CONSTRAINT DF_TentativaLoginInvalida_TentativasInvalidas DEFAULT (0),
        AtualizadoEmUtc DATETIME2(0) NOT NULL CONSTRAINT DF_TentativaLoginInvalida_AtualizadoEmUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_TentativaLoginInvalida PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_TentativaLoginInvalida_Email UNIQUE (Email),
        CONSTRAINT CK_TentativaLoginInvalida_TentativasInvalidas CHECK (TentativasInvalidas >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_TentativaLoginInvalida_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.TentativaLoginInvalida
        WITH CHECK ADD CONSTRAINT FK_TentativaLoginInvalida_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TentativaLoginInvalida_AtualizadoEmUtc' AND object_id = OBJECT_ID(N'dbo.TentativaLoginInvalida'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TentativaLoginInvalida_AtualizadoEmUtc
        ON dbo.TentativaLoginInvalida (AtualizadoEmUtc);
END;
GO




/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: Usuario */

IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Usuario_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DataNascimento DATE NULL,
        Nome NVARCHAR(150) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        SenhaHash NVARCHAR(500) NOT NULL,
        Ativo BIT NOT NULL CONSTRAINT DF_Usuario_Ativo DEFAULT (1),
        PrimeiroAcesso BIT NOT NULL CONSTRAINT DF_Usuario_PrimeiroAcesso DEFAULT (1),
        PerfilId INT NOT NULL CONSTRAINT DF_Usuario_PerfilId DEFAULT (1),
        CONSTRAINT PK_Usuario PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Usuario_Email UNIQUE (Email)
    );
END;
GO

IF COL_LENGTH('dbo.Usuario', 'DataNascimento') IS NULL
BEGIN
    ALTER TABLE dbo.Usuario
        ADD DataNascimento DATE NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Usuario_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Usuario
        WITH CHECK ADD CONSTRAINT FK_Usuario_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuario_UsuarioCadastroId' AND object_id = OBJECT_ID(N'dbo.Usuario'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Usuario_UsuarioCadastroId
        ON dbo.Usuario (UsuarioCadastroId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuario_Ativo_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.Usuario'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Usuario_Ativo_DataHoraCadastro
        ON dbo.Usuario (Ativo, DataHoraCadastro DESC)
        INCLUDE (Nome, Email, PerfilId, PrimeiroAcesso);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Usuario_Ativo_Nome_Email' AND object_id = OBJECT_ID(N'dbo.Usuario'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Usuario_Ativo_Nome_Email
        ON dbo.Usuario (Ativo, Nome, Email)
        INCLUDE (PerfilId, DataHoraCadastro, PrimeiroAcesso);
END;
GO

/* SEEDS - Usuario */
IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Usuario ON;

    INSERT INTO dbo.Usuario
    (
        Id,
        DataHoraCadastro,
        UsuarioCadastroId,
        DataNascimento,
        Nome,
        Email,
        SenhaHash,
        Ativo,
        PrimeiroAcesso,
        PerfilId
    )
    VALUES
    (
        1,
        '2026-01-01T00:00:00',
        1,
        NULL,
        N'Usuário',
        N'admin@core.com',
        N'PBKDF2$100000$DqVvtU2jQnWQTuqbL+H8aQ==$zvCjIqD8J/r93o4azALW2k8vIjoWtM5ikW7PKfY2PA8=',
        1,
        1,
        1
    );

    SET IDENTITY_INSERT dbo.Usuario OFF;
END;
GO


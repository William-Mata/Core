/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: Modulo */

IF OBJECT_ID(N'dbo.Modulo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Modulo
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Modulo_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Nome NVARCHAR(100) NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_Modulo_Status DEFAULT (1),
        CONSTRAINT PK_Modulo PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Modulo_Nome UNIQUE (Nome)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Modulo_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Modulo
        WITH CHECK ADD CONSTRAINT FK_Modulo_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* SEEDS - Modulo */
SET IDENTITY_INSERT dbo.Modulo ON;

MERGE dbo.Modulo AS target
USING
(
    VALUES
        (1, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, N'Geral', 1),
        (2, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, N'Administração', 1),
        (3, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, N'Financeiro', 1)
) AS source (Id, DataHoraCadastro, UsuarioCadastroId, Nome, Status)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        DataHoraCadastro = source.DataHoraCadastro,
        UsuarioCadastroId = source.UsuarioCadastroId,
        Nome = source.Nome,
        Status = source.Status
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, DataHoraCadastro, UsuarioCadastroId, Nome, Status)
    VALUES (source.Id, source.DataHoraCadastro, source.UsuarioCadastroId, source.Nome, source.Status);

SET IDENTITY_INSERT dbo.Modulo OFF;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Modulo WHERE Nome = N'Compras')
BEGIN
    INSERT INTO dbo.Modulo (DataHoraCadastro, UsuarioCadastroId, Nome, Status)
    VALUES (SYSUTCDATETIME(), 1, N'Compras', 1);
END;
GO


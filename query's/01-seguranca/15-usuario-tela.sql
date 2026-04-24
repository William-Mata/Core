/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: UsuarioTela */

IF OBJECT_ID(N'dbo.UsuarioTela', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioTela
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_UsuarioTela_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        UsuarioId INT NOT NULL,
        TelaId INT NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_UsuarioTela_Status DEFAULT (1),
        CONSTRAINT PK_UsuarioTela PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_UsuarioTela_UsuarioId_TelaId UNIQUE (UsuarioId, TelaId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioTela_Usuario_UsuarioId')
BEGIN
    ALTER TABLE dbo.UsuarioTela
        WITH CHECK ADD CONSTRAINT FK_UsuarioTela_Usuario_UsuarioId
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuario (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioTela_Tela_TelaId')
BEGIN
    ALTER TABLE dbo.UsuarioTela
        WITH CHECK ADD CONSTRAINT FK_UsuarioTela_Tela_TelaId
        FOREIGN KEY (TelaId) REFERENCES dbo.Tela (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioTela_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.UsuarioTela
        WITH CHECK ADD CONSTRAINT FK_UsuarioTela_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* SEEDS - UsuarioTela */
INSERT INTO dbo.UsuarioTela (DataHoraCadastro, UsuarioCadastroId, UsuarioId, TelaId, Status)
SELECT
    SYSUTCDATETIME(),
    1,
    1,
    t.Id,
    1
FROM dbo.Tela t
WHERE t.Status = 1
  AND EXISTS
  (
      SELECT 1
      FROM dbo.Usuario u
      WHERE u.Id = 1
        AND u.Ativo = 1
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.UsuarioTela ut
      WHERE ut.UsuarioId = 1
        AND ut.TelaId = t.Id
  );
GO


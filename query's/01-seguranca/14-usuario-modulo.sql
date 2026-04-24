/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: UsuarioModulo */

IF OBJECT_ID(N'dbo.UsuarioModulo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioModulo
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_UsuarioModulo_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        UsuarioId INT NOT NULL,
        ModuloId INT NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_UsuarioModulo_Status DEFAULT (1),
        CONSTRAINT PK_UsuarioModulo PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_UsuarioModulo_UsuarioId_ModuloId UNIQUE (UsuarioId, ModuloId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioModulo_Usuario_UsuarioId')
BEGIN
    ALTER TABLE dbo.UsuarioModulo
        WITH CHECK ADD CONSTRAINT FK_UsuarioModulo_Usuario_UsuarioId
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuario (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioModulo_Modulo_ModuloId')
BEGIN
    ALTER TABLE dbo.UsuarioModulo
        WITH CHECK ADD CONSTRAINT FK_UsuarioModulo_Modulo_ModuloId
        FOREIGN KEY (ModuloId) REFERENCES dbo.Modulo (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioModulo_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.UsuarioModulo
        WITH CHECK ADD CONSTRAINT FK_UsuarioModulo_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* SEEDS - UsuarioModulo */
INSERT INTO dbo.UsuarioModulo (DataHoraCadastro, UsuarioCadastroId, UsuarioId, ModuloId, Status)
SELECT
    SYSUTCDATETIME(),
    1,
    1,
    m.Id,
    1
FROM dbo.Modulo m
WHERE m.Status = 1
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
      FROM dbo.UsuarioModulo um
      WHERE um.UsuarioId = 1
        AND um.ModuloId = m.Id
  );
GO


/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: UsuarioFuncionalidade */

IF OBJECT_ID(N'dbo.UsuarioFuncionalidade', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsuarioFuncionalidade
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_UsuarioFuncionalidade_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        UsuarioId INT NOT NULL,
        FuncionalidadeId INT NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_UsuarioFuncionalidade_Status DEFAULT (1),
        CONSTRAINT PK_UsuarioFuncionalidade PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_UsuarioFuncionalidade_UsuarioId_FuncionalidadeId UNIQUE (UsuarioId, FuncionalidadeId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioFuncionalidade_Usuario_UsuarioId')
BEGIN
    ALTER TABLE dbo.UsuarioFuncionalidade
        WITH CHECK ADD CONSTRAINT FK_UsuarioFuncionalidade_Usuario_UsuarioId
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuario (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioFuncionalidade_Funcionalidade_FuncionalidadeId')
BEGIN
    ALTER TABLE dbo.UsuarioFuncionalidade
        WITH CHECK ADD CONSTRAINT FK_UsuarioFuncionalidade_Funcionalidade_FuncionalidadeId
        FOREIGN KEY (FuncionalidadeId) REFERENCES dbo.Funcionalidade (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UsuarioFuncionalidade_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.UsuarioFuncionalidade
        WITH CHECK ADD CONSTRAINT FK_UsuarioFuncionalidade_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* SEEDS - UsuarioFuncionalidade */
INSERT INTO dbo.UsuarioFuncionalidade (DataHoraCadastro, UsuarioCadastroId, UsuarioId, FuncionalidadeId, Status)
SELECT
    SYSUTCDATETIME(),
    1,
    1,
    f.Id,
    1
FROM dbo.Funcionalidade f
WHERE f.Status = 1
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
      FROM dbo.UsuarioFuncionalidade uf
      WHERE uf.UsuarioId = 1
        AND uf.FuncionalidadeId = f.Id
  );
GO


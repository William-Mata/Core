/*
Script mestre para criacao completa do banco Financeiro.
Executavel diretamente no SQL Server Management Studio.
Nao utiliza :r nem SQLCMD mode.
*/

IF DB_ID(N'Financeiro') IS NULL
BEGIN
    CREATE DATABASE [Financeiro];
END;
GO

USE [Financeiro];
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'dbo')
BEGIN
    EXEC(N'CREATE SCHEMA dbo');
END;
GO

/*
Ordem 01 - Seguranca e autenticacao
*/
IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Usuario_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Modulo_UsuarioCadastroId_Status' AND object_id = OBJECT_ID(N'dbo.Modulo'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Modulo_UsuarioCadastroId_Status
        ON dbo.Modulo (UsuarioCadastroId, Status)
        INCLUDE (Nome);
END;
GO

IF OBJECT_ID(N'dbo.Tela', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tela
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Tela_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ModuloId INT NOT NULL,
        Nome NVARCHAR(100) NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_Tela_Status DEFAULT (1),
        CONSTRAINT PK_Tela PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Tela_ModuloId_Nome UNIQUE (ModuloId, Nome)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Tela_Modulo_ModuloId')
BEGIN
    ALTER TABLE dbo.Tela
        WITH CHECK ADD CONSTRAINT FK_Tela_Modulo_ModuloId
        FOREIGN KEY (ModuloId) REFERENCES dbo.Modulo (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Tela_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Tela
        WITH CHECK ADD CONSTRAINT FK_Tela_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tela_ModuloId_Status' AND object_id = OBJECT_ID(N'dbo.Tela'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tela_ModuloId_Status
        ON dbo.Tela (ModuloId, Status)
        INCLUDE (Nome);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tela_UsuarioCadastroId' AND object_id = OBJECT_ID(N'dbo.Tela'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Tela_UsuarioCadastroId
        ON dbo.Tela (UsuarioCadastroId);
END;
GO

IF OBJECT_ID(N'dbo.Funcionalidade', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Funcionalidade
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Funcionalidade_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        TelaId INT NOT NULL,
        Nome NVARCHAR(100) NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_Funcionalidade_Status DEFAULT (1),
        CONSTRAINT PK_Funcionalidade PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Funcionalidade_TelaId_Nome UNIQUE (TelaId, Nome)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Funcionalidade_Tela_TelaId')
BEGIN
    ALTER TABLE dbo.Funcionalidade
        WITH CHECK ADD CONSTRAINT FK_Funcionalidade_Tela_TelaId
        FOREIGN KEY (TelaId) REFERENCES dbo.Tela (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Funcionalidade_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Funcionalidade
        WITH CHECK ADD CONSTRAINT FK_Funcionalidade_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Funcionalidade_TelaId_Status' AND object_id = OBJECT_ID(N'dbo.Funcionalidade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Funcionalidade_TelaId_Status
        ON dbo.Funcionalidade (TelaId, Status)
        INCLUDE (Nome);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Funcionalidade_UsuarioCadastroId' AND object_id = OBJECT_ID(N'dbo.Funcionalidade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Funcionalidade_UsuarioCadastroId
        ON dbo.Funcionalidade (UsuarioCadastroId);
END;
GO

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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UsuarioModulo_UsuarioId_Status' AND object_id = OBJECT_ID(N'dbo.UsuarioModulo'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_UsuarioModulo_UsuarioId_Status
        ON dbo.UsuarioModulo (UsuarioId, Status)
        INCLUDE (ModuloId);
END;
GO

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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UsuarioTela_UsuarioId_Status' AND object_id = OBJECT_ID(N'dbo.UsuarioTela'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_UsuarioTela_UsuarioId_Status
        ON dbo.UsuarioTela (UsuarioId, Status)
        INCLUDE (TelaId);
END;
GO

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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UsuarioFuncionalidade_UsuarioId_Status' AND object_id = OBJECT_ID(N'dbo.UsuarioFuncionalidade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_UsuarioFuncionalidade_UsuarioId_Status
        ON dbo.UsuarioFuncionalidade (UsuarioId, Status)
        INCLUDE (FuncionalidadeId);
END;
GO

IF OBJECT_ID(N'dbo.TR_Usuario_AfterInsert_PermissoesPadrao', N'TR') IS NOT NULL
BEGIN
    DROP TRIGGER dbo.TR_Usuario_AfterInsert_PermissoesPadrao;
END;
GO

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

SET IDENTITY_INSERT dbo.Modulo ON;

MERGE dbo.Modulo AS target
USING
(
    VALUES
        (1, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, N'Geral', 1),
        (2, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, N'Administracao', 1),
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

SET IDENTITY_INSERT dbo.Tela ON;

MERGE dbo.Tela AS target
USING
(
    VALUES
        (1, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Dashboard', 1),
        (2, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Painel do Usuario', 1),
        (3, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Lista de Amigos', 1),
        (4, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Convites', 1),
        (5, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Documentacao Modulo Geral', 1),
        (30, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Administracao', 1),
        (31, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Usuarios', 1),
        (33, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Documentos', 1),
        (34, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Avisos', 1),
        (35, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Documentacao Modulo Administracao', 1),
        (100, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Despesas', 1),
        (101, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Receitas', 1),
        (102, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Reembolso', 1),
        (103, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Contas Bancarias', 1),
        (104, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Cartoes de Credito', 1),
        (105, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Documentacao Modulo Financeiro', 1)
) AS source (Id, DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        DataHoraCadastro = source.DataHoraCadastro,
        UsuarioCadastroId = source.UsuarioCadastroId,
        ModuloId = source.ModuloId,
        Nome = source.Nome,
        Status = source.Status
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
    VALUES (source.Id, source.DataHoraCadastro, source.UsuarioCadastroId, source.ModuloId, source.Nome, source.Status);

SET IDENTITY_INSERT dbo.Tela OFF;
GO

SET IDENTITY_INSERT dbo.Funcionalidade ON;

MERGE dbo.Funcionalidade AS target
USING
(
    VALUES
        (1, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Visualizar', 1),
        (2, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 4, N'Criar', 1),
        (3, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Editar', 1),
        (4, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Excluir', 1)
) AS source (Id, DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        DataHoraCadastro = source.DataHoraCadastro,
        UsuarioCadastroId = source.UsuarioCadastroId,
        TelaId = source.TelaId,
        Nome = source.Nome,
        Status = source.Status
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status)
    VALUES (source.Id, source.DataHoraCadastro, source.UsuarioCadastroId, source.TelaId, source.Nome, source.Status);

SET IDENTITY_INSERT dbo.Funcionalidade OFF;
GO

/*
Ordem 02 - Cadastro
*/
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

/*
Ordem 03 - Conta bancaria
*/
IF OBJECT_ID(N'dbo.ContaBancaria', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaBancaria
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ContaBancaria_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(150) NOT NULL,
        Banco NVARCHAR(100) NOT NULL,
        Agencia NVARCHAR(30) NOT NULL,
        Numero NVARCHAR(30) NOT NULL,
        SaldoInicial DECIMAL(18,2) NOT NULL,
        SaldoAtual DECIMAL(18,2) NOT NULL,
        DataAbertura DATE NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_ContaBancaria_Status DEFAULT (N'Ativa'),
        CONSTRAINT PK_ContaBancaria PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ContaBancaria_Status CHECK (Status IN (N'Ativa', N'Inativa'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancaria_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ContaBancaria
        WITH CHECK ADD CONSTRAINT FK_ContaBancaria_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaBancaria_UsuarioCadastroId_Status' AND object_id = OBJECT_ID(N'dbo.ContaBancaria'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ContaBancaria_UsuarioCadastroId_Status
        ON dbo.ContaBancaria (UsuarioCadastroId, Status);
END;
GO

IF OBJECT_ID(N'dbo.ContaBancariaExtrato', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaBancariaExtrato
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ContaBancariaExtrato_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ContaBancariaId BIGINT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Tipo NVARCHAR(50) NOT NULL,
        Valor DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_ContaBancariaExtrato PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancariaExtrato_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.ContaBancariaExtrato
        WITH CHECK ADD CONSTRAINT FK_ContaBancariaExtrato_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancariaExtrato_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ContaBancariaExtrato
        WITH CHECK ADD CONSTRAINT FK_ContaBancariaExtrato_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaBancariaExtrato_ContaBancariaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ContaBancariaExtrato'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ContaBancariaExtrato_ContaBancariaId_DataHoraCadastro
        ON dbo.ContaBancariaExtrato (ContaBancariaId, DataHoraCadastro DESC);
END;
GO

IF OBJECT_ID(N'dbo.ContaBancariaLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContaBancariaLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ContaBancariaLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ContaBancariaId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_ContaBancariaLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ContaBancariaLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancariaLog_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.ContaBancariaLog
        WITH CHECK ADD CONSTRAINT FK_ContaBancariaLog_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContaBancariaLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ContaBancariaLog
        WITH CHECK ADD CONSTRAINT FK_ContaBancariaLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ContaBancariaLog_ContaBancariaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ContaBancariaLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ContaBancariaLog_ContaBancariaId_DataHoraCadastro
        ON dbo.ContaBancariaLog (ContaBancariaId, DataHoraCadastro DESC);
END;
GO

/*
Ordem 04 - Cartao
*/
IF OBJECT_ID(N'dbo.Cartao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cartao
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Cartao_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(150) NOT NULL,
        Bandeira NVARCHAR(100) NOT NULL,
        Tipo NVARCHAR(20) NOT NULL CONSTRAINT DF_Cartao_Tipo DEFAULT (N'Credito'),
        Limite DECIMAL(18,2) NULL,
        SaldoDisponivel DECIMAL(18,2) NOT NULL,
        DiaVencimento DATE NULL,
        DataVencimentoCartao DATE NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Cartao_Status DEFAULT (N'Ativo'),
        CONSTRAINT PK_Cartao PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Cartao_Tipo CHECK (Tipo IN (N'Credito', N'Debito')),
        CONSTRAINT CK_Cartao_Status CHECK (Status IN (N'Ativo', N'Inativo'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Cartao_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Cartao
        WITH CHECK ADD CONSTRAINT FK_Cartao_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cartao_UsuarioCadastroId_Status' AND object_id = OBJECT_ID(N'dbo.Cartao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Cartao_UsuarioCadastroId_Status
        ON dbo.Cartao (UsuarioCadastroId, Status);
END;
GO

IF OBJECT_ID(N'dbo.CartaoLancamento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartaoLancamento
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_CartaoLancamento_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        CartaoId BIGINT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Valor DECIMAL(18,2) NOT NULL,
        CONSTRAINT PK_CartaoLancamento PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartaoLancamento_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.CartaoLancamento
        WITH CHECK ADD CONSTRAINT FK_CartaoLancamento_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartaoLancamento_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.CartaoLancamento
        WITH CHECK ADD CONSTRAINT FK_CartaoLancamento_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CartaoLancamento_CartaoId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.CartaoLancamento'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CartaoLancamento_CartaoId_DataHoraCadastro
        ON dbo.CartaoLancamento (CartaoId, DataHoraCadastro DESC);
END;
GO

IF OBJECT_ID(N'dbo.CartaoLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartaoLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_CartaoLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        CartaoId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_CartaoLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_CartaoLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartaoLog_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.CartaoLog
        WITH CHECK ADD CONSTRAINT FK_CartaoLog_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CartaoLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.CartaoLog
        WITH CHECK ADD CONSTRAINT FK_CartaoLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CartaoLog_CartaoId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.CartaoLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CartaoLog_CartaoId_DataHoraCadastro
        ON dbo.CartaoLog (CartaoId, DataHoraCadastro DESC);
END;
GO

/*
Ordem 05 - Despesa
*/
IF OBJECT_ID(N'dbo.Despesa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Despesa
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Despesa_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Observacao NVARCHAR(1000) NULL,
        DataLancamento DATE NOT NULL,
        DataVencimento DATE NOT NULL,
        DataEfetivacao DATE NULL,
        TipoDespesa NVARCHAR(50) NOT NULL,
        TipoPagamento NVARCHAR(50) NOT NULL,
        Recorrencia NVARCHAR(20) NOT NULL CONSTRAINT DF_Despesa_Recorrencia DEFAULT (N'Unica'),
        RecorrenciaFixa BIT NOT NULL CONSTRAINT DF_Despesa_RecorrenciaFixa DEFAULT (0),
        QuantidadeRecorrencia INT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        ValorLiquido DECIMAL(18,2) NOT NULL,
        Desconto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Despesa_Desconto DEFAULT (0),
        Acrescimo DECIMAL(18,2) NOT NULL CONSTRAINT DF_Despesa_Acrescimo DEFAULT (0),
        Imposto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Despesa_Imposto DEFAULT (0),
        Juros DECIMAL(18,2) NOT NULL CONSTRAINT DF_Despesa_Juros DEFAULT (0),
        ValorEfetivacao DECIMAL(18,2) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Despesa_Status DEFAULT (N'Pendente'),
        CONSTRAINT PK_Despesa PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Despesa_Recorrencia CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual')),
        CONSTRAINT CK_Despesa_RecorrenciaFixa CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0),
        CONSTRAINT CK_Despesa_QuantidadeRecorrencia CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0),
        CONSTRAINT CK_Despesa_Status CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada')),
        CONSTRAINT CK_Despesa_TipoDespesa CHECK (TipoDespesa IN (N'alimentacao', N'transporte', N'moradia', N'lazer', N'saude', N'educacao', N'servicos')),
        CONSTRAINT CK_Despesa_TipoPagamento CHECK (TipoPagamento IN (N'pix', N'cartaoCredito', N'cartaoDebito', N'boleto', N'transferencia', N'dinheiro'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_UsuarioCadastroId_Status_DataVencimento' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_UsuarioCadastroId_Status_DataVencimento
        ON dbo.Despesa (UsuarioCadastroId, Status, DataVencimento);
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
        CONSTRAINT PK_DespesaAmigoRateio PRIMARY KEY CLUSTERED (Id)
    );
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAmigoRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAmigoRateio_DespesaId
        ON dbo.DespesaAmigoRateio (DespesaId);
END;
GO

IF OBJECT_ID(N'dbo.DespesaTipoRateio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaTipoRateio
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaTipoRateio_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        TipoRateio NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_DespesaTipoRateio PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaTipoRateio_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaTipoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaTipoRateio_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaTipoRateio_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaTipoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaTipoRateio_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaTipoRateio_DespesaId' AND object_id = OBJECT_ID(N'dbo.DespesaTipoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaTipoRateio_DespesaId
        ON dbo.DespesaTipoRateio (DespesaId);
END;
GO

IF OBJECT_ID(N'dbo.DespesaLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DespesaLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DespesaLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        DespesaId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_DespesaLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_DespesaLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaLog_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.DespesaLog
        WITH CHECK ADD CONSTRAINT FK_DespesaLog_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DespesaLog
        WITH CHECK ADD CONSTRAINT FK_DespesaLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaLog_DespesaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.DespesaLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaLog_DespesaId_DataHoraCadastro
        ON dbo.DespesaLog (DespesaId, DataHoraCadastro DESC);
END;
GO

/*
Ordem 06 - Receita
*/
IF OBJECT_ID(N'dbo.Receita', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Receita
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Receita_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(200) NOT NULL,
        Observacao NVARCHAR(1000) NULL,
        DataLancamento DATE NOT NULL,
        DataVencimento DATE NOT NULL,
        DataEfetivacao DATE NULL,
        TipoReceita NVARCHAR(50) NOT NULL,
        TipoRecebimento NVARCHAR(50) NOT NULL,
        Recorrencia NVARCHAR(20) NOT NULL CONSTRAINT DF_Receita_Recorrencia DEFAULT (N'Unica'),
        RecorrenciaFixa BIT NOT NULL CONSTRAINT DF_Receita_RecorrenciaFixa DEFAULT (0),
        QuantidadeRecorrencia INT NULL,
        ValorTotal DECIMAL(18,2) NOT NULL,
        ValorLiquido DECIMAL(18,2) NOT NULL,
        Desconto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Desconto DEFAULT (0),
        Acrescimo DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Acrescimo DEFAULT (0),
        Imposto DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Imposto DEFAULT (0),
        Juros DECIMAL(18,2) NOT NULL CONSTRAINT DF_Receita_Juros DEFAULT (0),
        ValorEfetivacao DECIMAL(18,2) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_Receita_Status DEFAULT (N'Pendente'),
        ContaBancariaId BIGINT NULL,
        CONSTRAINT PK_Receita PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Receita_Recorrencia CHECK (Recorrencia IN (N'Unica', N'Diaria', N'Semanal', N'Quinzenal', N'Mensal', N'Trimestral', N'Semestral', N'Anual')),
        CONSTRAINT CK_Receita_RecorrenciaFixa CHECK (Recorrencia <> N'Unica' OR RecorrenciaFixa = 0),
        CONSTRAINT CK_Receita_QuantidadeRecorrencia CHECK (QuantidadeRecorrencia IS NULL OR QuantidadeRecorrencia > 0),
        CONSTRAINT CK_Receita_Status CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada')),
        CONSTRAINT CK_Receita_TipoReceita CHECK (TipoReceita IN (N'salario', N'freelance', N'reembolso', N'investimento', N'bonus', N'outros')),
        CONSTRAINT CK_Receita_TipoRecebimento CHECK (TipoRecebimento IN (N'pix', N'transferencia', N'contaCorrente', N'dinheiro', N'boleto'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_UsuarioCadastroId_Status_DataVencimento' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_UsuarioCadastroId_Status_DataVencimento
        ON dbo.Receita (UsuarioCadastroId, Status, DataVencimento);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ContaBancariaId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ContaBancariaId
        ON dbo.Receita (ContaBancariaId);
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAmigoRateio_ReceitaId' AND object_id = OBJECT_ID(N'dbo.ReceitaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAmigoRateio_ReceitaId
        ON dbo.ReceitaAmigoRateio (ReceitaId);
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

IF OBJECT_ID(N'dbo.ReceitaLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceitaLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ReceitaLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ReceitaId BIGINT NOT NULL,
        Acao NVARCHAR(20) NOT NULL,
        Descricao NVARCHAR(500) NOT NULL,
        CONSTRAINT PK_ReceitaLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ReceitaLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaLog_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.ReceitaLog
        WITH CHECK ADD CONSTRAINT FK_ReceitaLog_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ReceitaLog
        WITH CHECK ADD CONSTRAINT FK_ReceitaLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaLog_ReceitaId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ReceitaLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaLog_ReceitaId_DataHoraCadastro
        ON dbo.ReceitaLog (ReceitaId, DataHoraCadastro DESC);
END;
GO

/*
Ordem 07 - Documentos
*/
IF OBJECT_ID(N'dbo.Documento', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documento
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Documento PRIMARY KEY,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Documento_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        NomeArquivo NVARCHAR(260) NOT NULL,
        CaminhoArquivo NVARCHAR(1000) NOT NULL,
        ContentType NVARCHAR(200) NULL,
        TamanhoBytes BIGINT NOT NULL,
        DespesaId BIGINT NULL,
        ReceitaId BIGINT NULL,
        ReembolsoId BIGINT NULL,
        CONSTRAINT CK_Documento_VinculoUnico CHECK (
            (CASE WHEN DespesaId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN ReceitaId IS NULL THEN 0 ELSE 1 END) +
            (CASE WHEN ReembolsoId IS NULL THEN 0 ELSE 1 END) = 1
        )
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Documento_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Documento
        WITH CHECK ADD CONSTRAINT FK_Documento_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Documento_Despesa_DespesaId')
BEGIN
    ALTER TABLE dbo.Documento
        WITH CHECK ADD CONSTRAINT FK_Documento_Despesa_DespesaId
        FOREIGN KEY (DespesaId) REFERENCES dbo.Despesa (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Documento_Receita_ReceitaId')
BEGIN
    ALTER TABLE dbo.Documento
        WITH CHECK ADD CONSTRAINT FK_Documento_Receita_ReceitaId
        FOREIGN KEY (ReceitaId) REFERENCES dbo.Receita (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Documento_Reembolso_ReembolsoId')
BEGIN
    ALTER TABLE dbo.Documento
        WITH CHECK ADD CONSTRAINT FK_Documento_Reembolso_ReembolsoId
        FOREIGN KEY (ReembolsoId) REFERENCES dbo.Reembolso (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documento_DespesaId' AND object_id = OBJECT_ID(N'dbo.Documento'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documento_DespesaId ON dbo.Documento (DespesaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documento_ReceitaId' AND object_id = OBJECT_ID(N'dbo.Documento'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documento_ReceitaId ON dbo.Documento (ReceitaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Documento_ReembolsoId' AND object_id = OBJECT_ID(N'dbo.Documento'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documento_ReembolsoId ON dbo.Documento (ReembolsoId);
END;
GO



/*
Ordem 14 - Amizade e aprovacao de rateio
*/
IF OBJECT_ID(N'dbo.ConviteAmizade', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConviteAmizade
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ConviteAmizade_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        UsuarioOrigemId INT NOT NULL,
        UsuarioDestinoId INT NOT NULL,
        Mensagem NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_ConviteAmizade_Status DEFAULT (N'Pendente'),
        DataHoraResposta DATETIME2(0) NULL,
        CONSTRAINT PK_ConviteAmizade PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ConviteAmizade_Status CHECK (Status IN (N'Pendente', N'Aceito', N'Rejeitado'))
    );
END;
GO

IF COL_LENGTH(N'dbo.ConviteAmizade', N'Mensagem') IS NULL
BEGIN
    ALTER TABLE dbo.ConviteAmizade
        ADD Mensagem NVARCHAR(500) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConviteAmizade_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ConviteAmizade
        WITH CHECK ADD CONSTRAINT FK_ConviteAmizade_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConviteAmizade_Usuario_UsuarioOrigemId')
BEGIN
    ALTER TABLE dbo.ConviteAmizade
        WITH CHECK ADD CONSTRAINT FK_ConviteAmizade_Usuario_UsuarioOrigemId
        FOREIGN KEY (UsuarioOrigemId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConviteAmizade_Usuario_UsuarioDestinoId')
BEGIN
    ALTER TABLE dbo.ConviteAmizade
        WITH CHECK ADD CONSTRAINT FK_ConviteAmizade_Usuario_UsuarioDestinoId
        FOREIGN KEY (UsuarioDestinoId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ConviteAmizade_UsuarioOrigemId_UsuarioDestinoId_Status' AND object_id = OBJECT_ID(N'dbo.ConviteAmizade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ConviteAmizade_UsuarioOrigemId_UsuarioDestinoId_Status
        ON dbo.ConviteAmizade (UsuarioOrigemId, UsuarioDestinoId, Status);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ConviteAmizade_UsuarioDestinoId_Status_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ConviteAmizade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ConviteAmizade_UsuarioDestinoId_Status_DataHoraCadastro
        ON dbo.ConviteAmizade (UsuarioDestinoId, Status, DataHoraCadastro DESC);
END;
GO

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

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Amizade_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Amizade
        WITH CHECK ADD CONSTRAINT FK_Amizade_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Amizade_Usuario_UsuarioAId')
BEGIN
    ALTER TABLE dbo.Amizade
        WITH CHECK ADD CONSTRAINT FK_Amizade_Usuario_UsuarioAId
        FOREIGN KEY (UsuarioAId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Amizade_Usuario_UsuarioBId')
BEGIN
    ALTER TABLE dbo.Amizade
        WITH CHECK ADD CONSTRAINT FK_Amizade_Usuario_UsuarioBId
        FOREIGN KEY (UsuarioBId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Amizade_UsuarioAId_UsuarioBId' AND object_id = OBJECT_ID(N'dbo.Amizade'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Amizade_UsuarioAId_UsuarioBId
        ON dbo.Amizade (UsuarioAId, UsuarioBId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Amizade_UsuarioAId' AND object_id = OBJECT_ID(N'dbo.Amizade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Amizade_UsuarioAId
        ON dbo.Amizade (UsuarioAId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Amizade_UsuarioBId' AND object_id = OBJECT_ID(N'dbo.Amizade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Amizade_UsuarioBId
        ON dbo.Amizade (UsuarioBId);
END;
GO

IF COL_LENGTH(N'dbo.Despesa', N'DespesaOrigemId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD DespesaOrigemId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Despesa_DespesaOrigemId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Despesa_DespesaOrigemId
        FOREIGN KEY (DespesaOrigemId) REFERENCES dbo.Despesa (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_DespesaOrigemId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_DespesaOrigemId
        ON dbo.Despesa (DespesaOrigemId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_UsuarioCadastroId_Status_DespesaOrigemId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_UsuarioCadastroId_Status_DespesaOrigemId
        ON dbo.Despesa (UsuarioCadastroId, Status, DespesaOrigemId)
        INCLUDE (DataLancamento);
END;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Status' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa DROP CONSTRAINT CK_Despesa_Status;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Despesa_Status' AND parent_object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT CK_Despesa_Status
        CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada', N'PendenteAprovacao', N'Rejeitado'));
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'ReceitaOrigemId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD ReceitaOrigemId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Receita_ReceitaOrigemId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Receita_ReceitaOrigemId
        FOREIGN KEY (ReceitaOrigemId) REFERENCES dbo.Receita (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_ReceitaOrigemId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_ReceitaOrigemId
        ON dbo.Receita (ReceitaOrigemId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_UsuarioCadastroId_Status_ReceitaOrigemId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_UsuarioCadastroId_Status_ReceitaOrigemId
        ON dbo.Receita (UsuarioCadastroId, Status, ReceitaOrigemId)
        INCLUDE (DataLancamento);
END;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Status' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita DROP CONSTRAINT CK_Receita_Status;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Receita_Status' AND parent_object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT CK_Receita_Status
        CHECK (Status IN (N'Pendente', N'Efetivada', N'Cancelada', N'PendenteAprovacao', N'Rejeitado'));
END;
GO

IF COL_LENGTH(N'dbo.DespesaAmigoRateio', N'AmigoId') IS NULL
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio ADD AmigoId INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.ReceitaAmigoRateio', N'AmigoId') IS NULL
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio ADD AmigoId INT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DespesaAmigoRateio_AmigoId' AND object_id = OBJECT_ID(N'dbo.DespesaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DespesaAmigoRateio_AmigoId
        ON dbo.DespesaAmigoRateio (AmigoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceitaAmigoRateio_AmigoId' AND object_id = OBJECT_ID(N'dbo.ReceitaAmigoRateio'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ReceitaAmigoRateio_AmigoId
        ON dbo.ReceitaAmigoRateio (AmigoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DespesaAmigoRateio_Usuario_AmigoId')
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.DespesaAmigoRateio r
       LEFT JOIN dbo.Usuario u ON u.Id = r.AmigoId
       WHERE r.AmigoId IS NOT NULL
         AND u.Id IS NULL
   )
BEGIN
    ALTER TABLE dbo.DespesaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_DespesaAmigoRateio_Usuario_AmigoId
        FOREIGN KEY (AmigoId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceitaAmigoRateio_Usuario_AmigoId')
   AND NOT EXISTS
   (
       SELECT 1
       FROM dbo.ReceitaAmigoRateio r
       LEFT JOIN dbo.Usuario u ON u.Id = r.AmigoId
       WHERE r.AmigoId IS NOT NULL
         AND u.Id IS NULL
   )
BEGIN
    ALTER TABLE dbo.ReceitaAmigoRateio
        WITH CHECK ADD CONSTRAINT FK_ReceitaAmigoRateio_Usuario_AmigoId
        FOREIGN KEY (AmigoId) REFERENCES dbo.Usuario (Id);
END;
GO

/*
Ordem 15 - Vinculo de meio financeiro em despesa/receita
*/
IF COL_LENGTH(N'dbo.Despesa', N'ContaBancariaId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD ContaBancariaId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Despesa', N'CartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD CartaoId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'CartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD CartaoId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_ContaBancaria_ContaBancariaId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_ContaBancaria_ContaBancariaId
        FOREIGN KEY (ContaBancariaId) REFERENCES dbo.ContaBancaria (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_ContaBancariaId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_ContaBancariaId ON dbo.Despesa (ContaBancariaId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_CartaoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_CartaoId ON dbo.Despesa (CartaoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_CartaoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_CartaoId ON dbo.Receita (CartaoId);
END;
GO

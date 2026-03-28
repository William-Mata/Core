/*
Ordem sugerida: 01
Objetivo: criar objetos de seguranca e autenticacao.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

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

IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Usuario ON;

    INSERT INTO dbo.Usuario
    (
        Id,
        DataHoraCadastro,
        UsuarioCadastroId,
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
        N'Usuario',
        N'admin@core.com',
        N'PBKDF2$100000$DqVvtU2jQnWQTuqbL+H8aQ==$zvCjIqD8J/r93o4azALW2k8vIjoWtM5ikW7PKfY2PA8=',
        1,
        1,
        1
    );

    SET IDENTITY_INSERT dbo.Usuario OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Modulo WHERE Id = 2)
BEGIN
    SET IDENTITY_INSERT dbo.Modulo ON;

    INSERT INTO dbo.Modulo (Id, DataHoraCadastro, UsuarioCadastroId, Nome, Status)
    VALUES
        (2, '2026-01-01T00:00:00', 1, N'financeiro', 1),
        (3, '2026-01-01T00:00:00', 1, N'administracao', 1);

    SET IDENTITY_INSERT dbo.Modulo OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Tela WHERE Id = 2)
BEGIN
    SET IDENTITY_INSERT dbo.Tela ON;

    INSERT INTO dbo.Tela (Id, DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
    VALUES
        (2, '2026-01-01T00:00:00', 1, 2, N'Despesas', 1),
        (3, '2026-01-01T00:00:00', 1, 2, N'Receitas', 1),
        (4, '2026-01-01T00:00:00', 1, 3, N'Usuarios', 1);

    SET IDENTITY_INSERT dbo.Tela OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE Id = 2)
BEGIN
    SET IDENTITY_INSERT dbo.Funcionalidade ON;

    INSERT INTO dbo.Funcionalidade (Id, DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status)
    VALUES
        (2, '2026-01-01T00:00:00', 1, 2, N'editar', 1),
        (3, '2026-01-01T00:00:00', 1, 2, N'visualizar', 1),
        (4, '2026-01-01T00:00:00', 1, 4, N'criar', 1);

    SET IDENTITY_INSERT dbo.Funcionalidade OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioModulo WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.UsuarioModulo ON;

    INSERT INTO dbo.UsuarioModulo (Id, DataHoraCadastro, UsuarioCadastroId, UsuarioId, ModuloId, Status)
    VALUES
        (1, '2026-01-01T00:00:00', 1, 1, 2, 1),
        (2, '2026-01-01T00:00:00', 1, 1, 3, 1);

    SET IDENTITY_INSERT dbo.UsuarioModulo OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioTela WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.UsuarioTela ON;

    INSERT INTO dbo.UsuarioTela (Id, DataHoraCadastro, UsuarioCadastroId, UsuarioId, TelaId, Status)
    VALUES
        (1, '2026-01-01T00:00:00', 1, 1, 2, 1),
        (2, '2026-01-01T00:00:00', 1, 1, 3, 1),
        (3, '2026-01-01T00:00:00', 1, 1, 4, 1);

    SET IDENTITY_INSERT dbo.UsuarioTela OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.UsuarioFuncionalidade WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.UsuarioFuncionalidade ON;

    INSERT INTO dbo.UsuarioFuncionalidade (Id, DataHoraCadastro, UsuarioCadastroId, UsuarioId, FuncionalidadeId, Status)
    VALUES
        (1, '2026-01-01T00:00:00', 1, 1, 2, 1),
        (2, '2026-01-01T00:00:00', 1, 1, 3, 1),
        (3, '2026-01-01T00:00:00', 1, 1, 4, 1);

    SET IDENTITY_INSERT dbo.UsuarioFuncionalidade OFF;
END;
GO

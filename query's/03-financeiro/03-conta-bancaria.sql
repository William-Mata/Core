/*
Ordem sugerida: 03
Objetivo: criar objetos de conta bancaria, extrato e log.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

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

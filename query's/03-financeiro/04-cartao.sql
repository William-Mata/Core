/*
Ordem sugerida: 04
Objetivo: criar objetos de cartao, lancamento e log.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

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

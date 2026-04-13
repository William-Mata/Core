/*
Ordem sugerida: 20
Objetivo: criar agregador FaturaCartao e vinculos em despesa/receita/reembolso.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

IF OBJECT_ID(N'dbo.FaturaCartao', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FaturaCartao
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_FaturaCartao_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        CartaoId BIGINT NOT NULL,
        Competencia CHAR(7) NOT NULL,
        DataFechamento DATE NULL,
        DataEfetivacao DATE NULL,
        DataEstorno DATE NULL,
        ValorTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_FaturaCartao_ValorTotal DEFAULT (0),
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_FaturaCartao_Status DEFAULT (N'Aberta'),
        CONSTRAINT PK_FaturaCartao PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_FaturaCartao_Status CHECK (Status IN (N'Aberta', N'Fechada', N'Efetivada', N'Estornada'))
    );
END;
GO

IF COL_LENGTH(N'dbo.Despesa', N'FaturaCartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Despesa ADD FaturaCartaoId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Receita', N'FaturaCartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Receita ADD FaturaCartaoId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Reembolso', N'CartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso ADD CartaoId BIGINT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Reembolso', N'FaturaCartaoId') IS NULL
BEGIN
    ALTER TABLE dbo.Reembolso ADD FaturaCartaoId BIGINT NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FaturaCartao_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.FaturaCartao
        WITH CHECK ADD CONSTRAINT FK_FaturaCartao_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FaturaCartao_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.FaturaCartao
        WITH CHECK ADD CONSTRAINT FK_FaturaCartao_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Despesa_FaturaCartao_FaturaCartaoId')
BEGIN
    ALTER TABLE dbo.Despesa
        WITH CHECK ADD CONSTRAINT FK_Despesa_FaturaCartao_FaturaCartaoId
        FOREIGN KEY (FaturaCartaoId) REFERENCES dbo.FaturaCartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Receita_FaturaCartao_FaturaCartaoId')
BEGIN
    ALTER TABLE dbo.Receita
        WITH CHECK ADD CONSTRAINT FK_Receita_FaturaCartao_FaturaCartaoId
        FOREIGN KEY (FaturaCartaoId) REFERENCES dbo.FaturaCartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Reembolso_Cartao_CartaoId')
BEGIN
    ALTER TABLE dbo.Reembolso
        WITH CHECK ADD CONSTRAINT FK_Reembolso_Cartao_CartaoId
        FOREIGN KEY (CartaoId) REFERENCES dbo.Cartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Reembolso_FaturaCartao_FaturaCartaoId')
BEGIN
    ALTER TABLE dbo.Reembolso
        WITH CHECK ADD CONSTRAINT FK_Reembolso_FaturaCartao_FaturaCartaoId
        FOREIGN KEY (FaturaCartaoId) REFERENCES dbo.FaturaCartao (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FaturaCartao_UsuarioCadastroId_CartaoId_Competencia' AND object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_FaturaCartao_UsuarioCadastroId_CartaoId_Competencia
        ON dbo.FaturaCartao (UsuarioCadastroId, CartaoId, Competencia);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FaturaCartao_CartaoId_Competencia' AND object_id = OBJECT_ID(N'dbo.FaturaCartao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_FaturaCartao_CartaoId_Competencia
        ON dbo.FaturaCartao (CartaoId, Competencia);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Despesa_FaturaCartaoId' AND object_id = OBJECT_ID(N'dbo.Despesa'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Despesa_FaturaCartaoId
        ON dbo.Despesa (FaturaCartaoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Receita_FaturaCartaoId' AND object_id = OBJECT_ID(N'dbo.Receita'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Receita_FaturaCartaoId
        ON dbo.Receita (FaturaCartaoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_CartaoId' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_CartaoId
        ON dbo.Reembolso (CartaoId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Reembolso_FaturaCartaoId' AND object_id = OBJECT_ID(N'dbo.Reembolso'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Reembolso_FaturaCartaoId
        ON dbo.Reembolso (FaturaCartaoId);
END;
GO


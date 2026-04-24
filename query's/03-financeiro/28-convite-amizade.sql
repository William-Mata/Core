/*
Scripts organizados por tabela do modulo Financeiro.
Tabela de acao: ConviteAmizade
Fonte: query's/03-financeiro/*.sql (consolidado em ordem original).
*/

USE [Financeiro];
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 2) */
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

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 3) */
IF COL_LENGTH(N'dbo.ConviteAmizade', N'Mensagem') IS NULL
BEGIN
    ALTER TABLE dbo.ConviteAmizade
        ADD Mensagem NVARCHAR(500) NULL;
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 4) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConviteAmizade_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ConviteAmizade
        WITH CHECK ADD CONSTRAINT FK_ConviteAmizade_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 5) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConviteAmizade_Usuario_UsuarioOrigemId')
BEGIN
    ALTER TABLE dbo.ConviteAmizade
        WITH CHECK ADD CONSTRAINT FK_ConviteAmizade_Usuario_UsuarioOrigemId
        FOREIGN KEY (UsuarioOrigemId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 6) */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConviteAmizade_Usuario_UsuarioDestinoId')
BEGIN
    ALTER TABLE dbo.ConviteAmizade
        WITH CHECK ADD CONSTRAINT FK_ConviteAmizade_Usuario_UsuarioDestinoId
        FOREIGN KEY (UsuarioDestinoId) REFERENCES dbo.Usuario (Id);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 7) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ConviteAmizade_UsuarioOrigemId_UsuarioDestinoId_Status' AND object_id = OBJECT_ID(N'dbo.ConviteAmizade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ConviteAmizade_UsuarioOrigemId_UsuarioDestinoId_Status
        ON dbo.ConviteAmizade (UsuarioOrigemId, UsuarioDestinoId, Status);
END;
GO

/* Origem: 14-amizade-aprovacao-rateio.sql (bloco 8) */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ConviteAmizade_UsuarioDestinoId_Status_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ConviteAmizade'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ConviteAmizade_UsuarioDestinoId_Status_DataHoraCadastro
        ON dbo.ConviteAmizade (UsuarioDestinoId, Status, DataHoraCadastro DESC);
END;
GO



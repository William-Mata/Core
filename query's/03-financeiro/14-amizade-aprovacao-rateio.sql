/*
Ordem sugerida: 14
Objetivo: adicionar amizade/convite e suporte a aprovacao de rateio em despesa/receita.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

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

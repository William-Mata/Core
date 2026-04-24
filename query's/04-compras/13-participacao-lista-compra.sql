/*
Scripts refatorados por tabela do modulo Compras.
Origem: query's/04-compras/25-compras.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: ParticipacaoListaCompra */

IF OBJECT_ID(N'dbo.ParticipacaoListaCompra', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParticipacaoListaCompra
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ParticipacaoListaCompra_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ListaCompraId BIGINT NOT NULL,
        UsuarioId INT NOT NULL,
        Papel NVARCHAR(20) NOT NULL CONSTRAINT DF_ParticipacaoListaCompra_Papel DEFAULT (N'CoProprietario'),
        Status BIT NOT NULL CONSTRAINT DF_ParticipacaoListaCompra_Status DEFAULT (1),
        CONSTRAINT PK_ParticipacaoListaCompra PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ParticipacaoListaCompra_Papel CHECK (Papel IN (N'Proprietario', N'CoProprietario', N'Leitor'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ParticipacaoListaCompra_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ParticipacaoListaCompra
        WITH CHECK ADD CONSTRAINT FK_ParticipacaoListaCompra_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ParticipacaoListaCompra_ListaCompra_ListaCompraId')
BEGIN
    ALTER TABLE dbo.ParticipacaoListaCompra
        WITH CHECK ADD CONSTRAINT FK_ParticipacaoListaCompra_ListaCompra_ListaCompraId
        FOREIGN KEY (ListaCompraId) REFERENCES dbo.ListaCompra (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ParticipacaoListaCompra_Usuario_UsuarioId')
BEGIN
    ALTER TABLE dbo.ParticipacaoListaCompra
        WITH CHECK ADD CONSTRAINT FK_ParticipacaoListaCompra_Usuario_UsuarioId
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_ParticipacaoListaCompra_ListaCompraId_UsuarioId' AND object_id = OBJECT_ID(N'dbo.ParticipacaoListaCompra'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_ParticipacaoListaCompra_ListaCompraId_UsuarioId
        ON dbo.ParticipacaoListaCompra (ListaCompraId, UsuarioId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ParticipacaoListaCompra_UsuarioId_Status' AND object_id = OBJECT_ID(N'dbo.ParticipacaoListaCompra'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ParticipacaoListaCompra_UsuarioId_Status
        ON dbo.ParticipacaoListaCompra (UsuarioId, Status)
        INCLUDE (ListaCompraId, Papel);
END;
GO



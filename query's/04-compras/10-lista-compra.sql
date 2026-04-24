/*
Scripts refatorados por tabela do modulo Compras.
Origem: query's/04-compras/25-compras.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: ListaCompra */

IF OBJECT_ID(N'dbo.ListaCompra', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ListaCompra
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ListaCompra_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        UsuarioProprietarioId INT NOT NULL,
        Nome NVARCHAR(120) NOT NULL,
        Categoria NVARCHAR(80) NOT NULL,
        Observacao NVARCHAR(500) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_ListaCompra_Status DEFAULT (N'Ativa'),
        DataHoraAtualizacao DATETIME2(0) NOT NULL CONSTRAINT DF_ListaCompra_DataHoraAtualizacao DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ListaCompra PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ListaCompra_Status CHECK (Status IN (N'Ativa', N'Arquivada'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ListaCompra_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ListaCompra
        WITH CHECK ADD CONSTRAINT FK_ListaCompra_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ListaCompra_Usuario_UsuarioProprietarioId')
BEGIN
    ALTER TABLE dbo.ListaCompra
        WITH CHECK ADD CONSTRAINT FK_ListaCompra_Usuario_UsuarioProprietarioId
        FOREIGN KEY (UsuarioProprietarioId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaCompra_UsuarioProprietarioId_Status' AND object_id = OBJECT_ID(N'dbo.ListaCompra'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ListaCompra_UsuarioProprietarioId_Status
        ON dbo.ListaCompra (UsuarioProprietarioId, Status)
        INCLUDE (Nome, Categoria, DataHoraAtualizacao);
END;
GO



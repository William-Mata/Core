/*
Scripts refatorados por tabela do modulo Compras.
Origem: query's/04-compras/25-compras.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: ListaCompraLog */

IF OBJECT_ID(N'dbo.ListaCompraLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ListaCompraLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ListaCompraLog_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ListaCompraId BIGINT NOT NULL,
        ItemListaCompraId BIGINT NULL,
        Acao NVARCHAR(20) NOT NULL CONSTRAINT DF_ListaCompraLog_Acao DEFAULT (N'Atualizacao'),
        Descricao NVARCHAR(500) NOT NULL,
        ValorAnterior NVARCHAR(500) NULL,
        ValorNovo NVARCHAR(500) NULL,
        CONSTRAINT PK_ListaCompraLog PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ListaCompraLog_Acao CHECK (Acao IN (N'Cadastro', N'Atualizacao', N'Exclusao'))
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ListaCompraLog_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ListaCompraLog
        WITH CHECK ADD CONSTRAINT FK_ListaCompraLog_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ListaCompraLog_ListaCompra_ListaCompraId')
BEGIN
    ALTER TABLE dbo.ListaCompraLog
        WITH CHECK ADD CONSTRAINT FK_ListaCompraLog_ListaCompra_ListaCompraId
        FOREIGN KEY (ListaCompraId) REFERENCES dbo.ListaCompra (Id)
        ON DELETE CASCADE;
END;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ListaCompraLog_ItemListaCompra_ItemListaCompraId')
BEGIN
    ALTER TABLE dbo.ListaCompraLog
        DROP CONSTRAINT FK_ListaCompraLog_ItemListaCompra_ItemListaCompraId;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ListaCompraLog_ItemListaCompra_ItemListaCompraId')
BEGIN
    ALTER TABLE dbo.ListaCompraLog
        WITH CHECK ADD CONSTRAINT FK_ListaCompraLog_ItemListaCompra_ItemListaCompraId
        FOREIGN KEY (ItemListaCompraId) REFERENCES dbo.ItemListaCompra (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaCompraLog_ListaCompraId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ListaCompraLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ListaCompraLog_ListaCompraId_DataHoraCadastro
        ON dbo.ListaCompraLog (ListaCompraId, DataHoraCadastro);
END;
GO



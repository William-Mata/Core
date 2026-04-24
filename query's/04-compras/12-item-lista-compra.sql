/*
Scripts refatorados por tabela do modulo Compras.
Origem: query's/04-compras/25-compras.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: ItemListaCompra */

IF OBJECT_ID(N'dbo.ItemListaCompra', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItemListaCompra
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ItemListaCompra_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ListaCompraId BIGINT NOT NULL,
        ProdutoId BIGINT NULL,
        Descricao NVARCHAR(180) NOT NULL,
        DescricaoNormalizada NVARCHAR(180) NOT NULL,
        Observacao NVARCHAR(500) NULL,
        Unidade NVARCHAR(20) NOT NULL CONSTRAINT DF_ItemListaCompra_Unidade DEFAULT (N'Unidade'),
        Quantidade DECIMAL(18,4) NOT NULL CONSTRAINT DF_ItemListaCompra_Quantidade DEFAULT (1),
        PrecoUnitario DECIMAL(18,4) NULL,
        ValorTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_ItemListaCompra_ValorTotal DEFAULT (0),
        EtiquetaCor NVARCHAR(40) NULL,
        Comprado BIT NOT NULL CONSTRAINT DF_ItemListaCompra_Comprado DEFAULT (0),
        DataHoraCompra DATETIME2(0) NULL,
        CONSTRAINT PK_ItemListaCompra PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ItemListaCompra_Unidade CHECK (Unidade IN (N'Unidade', N'Kg', N'G', N'Mg', N'L', N'Ml')),
        CONSTRAINT CK_ItemListaCompra_Quantidade CHECK (Quantidade > 0),
        CONSTRAINT CK_ItemListaCompra_PrecoUnitario CHECK (PrecoUnitario IS NULL OR PrecoUnitario >= 0),
        CONSTRAINT CK_ItemListaCompra_ValorTotal CHECK (ValorTotal >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ItemListaCompra_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.ItemListaCompra
        WITH CHECK ADD CONSTRAINT FK_ItemListaCompra_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ItemListaCompra_ListaCompra_ListaCompraId')
BEGIN
    ALTER TABLE dbo.ItemListaCompra
        WITH CHECK ADD CONSTRAINT FK_ItemListaCompra_ListaCompra_ListaCompraId
        FOREIGN KEY (ListaCompraId) REFERENCES dbo.ListaCompra (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ItemListaCompra_Produto_ProdutoId')
BEGIN
    ALTER TABLE dbo.ItemListaCompra
        WITH CHECK ADD CONSTRAINT FK_ItemListaCompra_Produto_ProdutoId
        FOREIGN KEY (ProdutoId) REFERENCES dbo.Produto (Id)
        ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemListaCompra_ListaCompraId_DescricaoNormalizada_Unidade' AND object_id = OBJECT_ID(N'dbo.ItemListaCompra'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ItemListaCompra_ListaCompraId_DescricaoNormalizada_Unidade
        ON dbo.ItemListaCompra (ListaCompraId, DescricaoNormalizada, Unidade);
END;
GO



/*
Scripts refatorados por tabela do modulo Compras.
Origem: query's/04-compras/25-compras.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: HistoricoProduto */

IF OBJECT_ID(N'dbo.HistoricoProduto', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistoricoProduto
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_HistoricoProduto_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ProdutoId BIGINT NOT NULL,
        ItemListaCompraId BIGINT NULL,
        Unidade NVARCHAR(20) NOT NULL CONSTRAINT DF_HistoricoProduto_Unidade DEFAULT (N'Unidade'),
        PrecoUnitario DECIMAL(18,4) NOT NULL,
        Origem NVARCHAR(20) NOT NULL CONSTRAINT DF_HistoricoProduto_Origem DEFAULT (N'Estimado'),
        CONSTRAINT PK_HistoricoProduto PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_HistoricoProduto_Unidade CHECK (Unidade IN (N'Unidade', N'Kg', N'G', N'Mg', N'L', N'Ml')),
        CONSTRAINT CK_HistoricoProduto_Origem CHECK (Origem IN (N'Estimado', N'Confirmado')),
        CONSTRAINT CK_HistoricoProduto_PrecoUnitario CHECK (PrecoUnitario > 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HistoricoProduto_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.HistoricoProduto
        WITH CHECK ADD CONSTRAINT FK_HistoricoProduto_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HistoricoProduto_Produto_ProdutoId')
BEGIN
    ALTER TABLE dbo.HistoricoProduto
        WITH CHECK ADD CONSTRAINT FK_HistoricoProduto_Produto_ProdutoId
        FOREIGN KEY (ProdutoId) REFERENCES dbo.Produto (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HistoricoProduto_ItemListaCompra_ItemListaCompraId')
BEGIN
    ALTER TABLE dbo.HistoricoProduto
        WITH CHECK ADD CONSTRAINT FK_HistoricoProduto_ItemListaCompra_ItemListaCompraId
        FOREIGN KEY (ItemListaCompraId) REFERENCES dbo.ItemListaCompra (Id)
        ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HistoricoProduto_ProdutoId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.HistoricoProduto'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_HistoricoProduto_ProdutoId_DataHoraCadastro
        ON dbo.HistoricoProduto (ProdutoId, DataHoraCadastro);
END;
GO



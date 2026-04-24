/*
Scripts refatorados por tabela do modulo Compras.
Origem: query's/04-compras/25-compras.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: DesejoCompra */

IF OBJECT_ID(N'dbo.DesejoCompra', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DesejoCompra
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_DesejoCompra_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ProdutoId BIGINT NULL,
        Descricao NVARCHAR(180) NOT NULL,
        DescricaoNormalizada NVARCHAR(180) NOT NULL,
        Observacao NVARCHAR(500) NULL,
        Unidade NVARCHAR(20) NOT NULL CONSTRAINT DF_DesejoCompra_Unidade DEFAULT (N'Unidade'),
        Quantidade DECIMAL(18,4) NOT NULL CONSTRAINT DF_DesejoCompra_Quantidade DEFAULT (1),
        PrecoEstimado DECIMAL(18,4) NULL,
        Convertido BIT NOT NULL CONSTRAINT DF_DesejoCompra_Convertido DEFAULT (0),
        DataHoraConversao DATETIME2(0) NULL,
        CONSTRAINT PK_DesejoCompra PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_DesejoCompra_Unidade CHECK (Unidade IN (N'Unidade', N'Kg', N'G', N'Mg', N'L', N'Ml')),
        CONSTRAINT CK_DesejoCompra_Quantidade CHECK (Quantidade > 0),
        CONSTRAINT CK_DesejoCompra_PrecoEstimado CHECK (PrecoEstimado IS NULL OR PrecoEstimado >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DesejoCompra_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.DesejoCompra
        WITH CHECK ADD CONSTRAINT FK_DesejoCompra_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DesejoCompra_Produto_ProdutoId')
BEGIN
    ALTER TABLE dbo.DesejoCompra
        WITH CHECK ADD CONSTRAINT FK_DesejoCompra_Produto_ProdutoId
        FOREIGN KEY (ProdutoId) REFERENCES dbo.Produto (Id)
        ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DesejoCompra_UsuarioCadastroId_Convertido' AND object_id = OBJECT_ID(N'dbo.DesejoCompra'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DesejoCompra_UsuarioCadastroId_Convertido
        ON dbo.DesejoCompra (UsuarioCadastroId, Convertido);
END;
GO



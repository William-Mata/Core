/*
Scripts refatorados por tabela do modulo Compras.
Origem: query's/04-compras/25-compras.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: Produto */

IF OBJECT_ID(N'dbo.Produto', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produto
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Produto_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        Descricao NVARCHAR(180) NOT NULL,
        DescricaoNormalizada NVARCHAR(180) NOT NULL,
        UnidadePadrao NVARCHAR(20) NOT NULL CONSTRAINT DF_Produto_UnidadePadrao DEFAULT (N'Unidade'),
        ObservacaoPadrao NVARCHAR(500) NULL,
        UltimoPrecoUnitario DECIMAL(18,4) NULL,
        DataHoraUltimoPreco DATETIME2(0) NULL,
        CONSTRAINT PK_Produto PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Produto_UnidadePadrao CHECK (UnidadePadrao IN (N'Unidade', N'Kg', N'G', N'Mg', N'L', N'Ml')),
        CONSTRAINT CK_Produto_UltimoPrecoUnitario CHECK (UltimoPrecoUnitario IS NULL OR UltimoPrecoUnitario >= 0)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Produto_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Produto
        WITH CHECK ADD CONSTRAINT FK_Produto_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Produto_DescricaoNormalizada_UnidadePadrao' AND object_id = OBJECT_ID(N'dbo.Produto'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Produto_DescricaoNormalizada_UnidadePadrao
        ON dbo.Produto (DescricaoNormalizada, UnidadePadrao);
END;
GO



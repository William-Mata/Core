/*
Ordem sugerida: 25
Objetivo: criar estruturas do modulo Compras (listas, desejos, produto, historico e logs).
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

/*
Permissoes base do modulo Compras
*/
DECLARE @ModuloComprasId INT;

SELECT @ModuloComprasId = Id
FROM dbo.Modulo
WHERE Nome = N'compras';

IF @ModuloComprasId IS NULL
BEGIN
    INSERT INTO dbo.Modulo (DataHoraCadastro, UsuarioCadastroId, Nome, Status)
    VALUES (SYSUTCDATETIME(), 1, N'compras', 1);

    SET @ModuloComprasId = SCOPE_IDENTITY();
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'ListasCompras')
BEGIN
    INSERT INTO dbo.Tela (DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
    VALUES (SYSUTCDATETIME(), 1, @ModuloComprasId, N'ListasCompras', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'DesejosCompra')
BEGIN
    INSERT INTO dbo.Tela (DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
    VALUES (SYSUTCDATETIME(), 1, @ModuloComprasId, N'DesejosCompra', 1);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'HistoricoPrecos')
BEGIN
    INSERT INTO dbo.Tela (DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
    VALUES (SYSUTCDATETIME(), 1, @ModuloComprasId, N'HistoricoPrecos', 1);
END;

DECLARE @TelaListasComprasId INT = (SELECT TOP 1 Id FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'ListasCompras');
DECLARE @TelaDesejosCompraId INT = (SELECT TOP 1 Id FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'DesejosCompra');
DECLARE @TelaHistoricoPrecosId INT = (SELECT TOP 1 Id FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'HistoricoPrecos');

IF @TelaListasComprasId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaListasComprasId AND Nome = N'visualizar')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaListasComprasId, N'visualizar', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaListasComprasId AND Nome = N'criar')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaListasComprasId, N'criar', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaListasComprasId AND Nome = N'editar')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaListasComprasId, N'editar', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaListasComprasId AND Nome = N'compartilhar')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaListasComprasId, N'compartilhar', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaListasComprasId AND Nome = N'acoes_em_lote')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaListasComprasId, N'acoes_em_lote', 1);
END;

IF @TelaDesejosCompraId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaDesejosCompraId AND Nome = N'visualizar')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaDesejosCompraId, N'visualizar', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaDesejosCompraId AND Nome = N'criar')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaDesejosCompraId, N'criar', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaDesejosCompraId AND Nome = N'editar')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaDesejosCompraId, N'editar', 1);
END;

IF @TelaHistoricoPrecosId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaHistoricoPrecosId AND Nome = N'visualizar')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaHistoricoPrecosId, N'visualizar', 1);
    IF NOT EXISTS (SELECT 1 FROM dbo.Funcionalidade WHERE TelaId = @TelaHistoricoPrecosId AND Nome = N'consultar_historico')
        INSERT INTO dbo.Funcionalidade (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status) VALUES (SYSUTCDATETIME(), 1, @TelaHistoricoPrecosId, N'consultar_historico', 1);
END;
GO

INSERT INTO dbo.UsuarioModulo (DataHoraCadastro, UsuarioCadastroId, UsuarioId, ModuloId, Status)
SELECT SYSUTCDATETIME(), 1, 1, m.Id, 1
FROM dbo.Modulo m
WHERE m.Nome = N'compras'
  AND EXISTS (SELECT 1 FROM dbo.Usuario u WHERE u.Id = 1 AND u.Ativo = 1)
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.UsuarioModulo um
      WHERE um.UsuarioId = 1
        AND um.ModuloId = m.Id
  );
GO

INSERT INTO dbo.UsuarioTela (DataHoraCadastro, UsuarioCadastroId, UsuarioId, TelaId, Status)
SELECT SYSUTCDATETIME(), 1, 1, t.Id, 1
FROM dbo.Tela t
JOIN dbo.Modulo m ON m.Id = t.ModuloId
WHERE m.Nome = N'compras'
  AND t.Status = 1
  AND EXISTS (SELECT 1 FROM dbo.Usuario u WHERE u.Id = 1 AND u.Ativo = 1)
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.UsuarioTela ut
      WHERE ut.UsuarioId = 1
        AND ut.TelaId = t.Id
  );
GO

INSERT INTO dbo.UsuarioFuncionalidade (DataHoraCadastro, UsuarioCadastroId, UsuarioId, FuncionalidadeId, Status)
SELECT SYSUTCDATETIME(), 1, 1, f.Id, 1
FROM dbo.Funcionalidade f
JOIN dbo.Tela t ON t.Id = f.TelaId
JOIN dbo.Modulo m ON m.Id = t.ModuloId
WHERE m.Nome = N'compras'
  AND f.Status = 1
  AND EXISTS (SELECT 1 FROM dbo.Usuario u WHERE u.Id = 1 AND u.Ativo = 1)
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.UsuarioFuncionalidade uf
      WHERE uf.UsuarioId = 1
        AND uf.FuncionalidadeId = f.Id
  );
GO

/*
Tabelas do dominio Compras
*/
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

IF OBJECT_ID(N'dbo.ParticipacaoListaCompra', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ParticipacaoListaCompra
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_ParticipacaoListaCompra_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ListaCompraId BIGINT NOT NULL,
        UsuarioId INT NOT NULL,
        Papel NVARCHAR(20) NOT NULL CONSTRAINT DF_ParticipacaoListaCompra_Papel DEFAULT (N'Editor'),
        Status BIT NOT NULL CONSTRAINT DF_ParticipacaoListaCompra_Status DEFAULT (1),
        CONSTRAINT PK_ParticipacaoListaCompra PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_ParticipacaoListaCompra_Papel CHECK (Papel IN (N'Proprietario', N'Editor', N'Leitor'))
    );
END;
GO

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

/*
Relacionamentos
*/
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

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Produto_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Produto
        WITH CHECK ADD CONSTRAINT FK_Produto_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
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

/*
Indices
*/
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaCompra_UsuarioProprietarioId_Status' AND object_id = OBJECT_ID(N'dbo.ListaCompra'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ListaCompra_UsuarioProprietarioId_Status
        ON dbo.ListaCompra (UsuarioProprietarioId, Status)
        INCLUDE (Nome, Categoria, DataHoraAtualizacao);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItemListaCompra_ListaCompraId_DescricaoNormalizada_Unidade' AND object_id = OBJECT_ID(N'dbo.ItemListaCompra'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ItemListaCompra_ListaCompraId_DescricaoNormalizada_Unidade
        ON dbo.ItemListaCompra (ListaCompraId, DescricaoNormalizada, Unidade);
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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Produto_DescricaoNormalizada_UnidadePadrao' AND object_id = OBJECT_ID(N'dbo.Produto'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Produto_DescricaoNormalizada_UnidadePadrao
        ON dbo.Produto (DescricaoNormalizada, UnidadePadrao);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DesejoCompra_UsuarioCadastroId_Convertido' AND object_id = OBJECT_ID(N'dbo.DesejoCompra'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DesejoCompra_UsuarioCadastroId_Convertido
        ON dbo.DesejoCompra (UsuarioCadastroId, Convertido);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HistoricoProduto_ProdutoId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.HistoricoProduto'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_HistoricoProduto_ProdutoId_DataHoraCadastro
        ON dbo.HistoricoProduto (ProdutoId, DataHoraCadastro);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaCompraLog_ListaCompraId_DataHoraCadastro' AND object_id = OBJECT_ID(N'dbo.ListaCompraLog'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ListaCompraLog_ListaCompraId_DataHoraCadastro
        ON dbo.ListaCompraLog (ListaCompraId, DataHoraCadastro);
END;
GO

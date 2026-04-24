/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: Tela */

IF OBJECT_ID(N'dbo.Tela', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tela
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Tela_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        ModuloId INT NOT NULL,
        Nome NVARCHAR(100) NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_Tela_Status DEFAULT (1),
        CONSTRAINT PK_Tela PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Tela_ModuloId_Nome UNIQUE (ModuloId, Nome)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Tela_Modulo_ModuloId')
BEGIN
    ALTER TABLE dbo.Tela
        WITH CHECK ADD CONSTRAINT FK_Tela_Modulo_ModuloId
        FOREIGN KEY (ModuloId) REFERENCES dbo.Modulo (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Tela_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Tela
        WITH CHECK ADD CONSTRAINT FK_Tela_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* SEEDS - Tela */
SET IDENTITY_INSERT dbo.Tela ON;

MERGE dbo.Tela AS target
USING
(
    VALUES
        (1, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Dashboard', 1),
        (2, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Painel do Usuário', 1),
        (3, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Lista de Amigos', 1),
        (4, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Convites', 1),
        (5, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 1, N'Documentação', 1),
        (30, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Administração', 1),
        (31, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Usuários', 1),
        (33, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Documentos', 1),
        (34, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Avisos', 1),
        (35, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Documentação', 1),
        (100, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Despesas', 1),
        (101, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Receitas', 1),
        (102, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Reembolsos', 1),
        (103, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Contas Bancárias', 1),
        (104, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Cartões', 1),
        (105, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 3, N'Documentação', 1)
) AS source (Id, DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        DataHoraCadastro = source.DataHoraCadastro,
        UsuarioCadastroId = source.UsuarioCadastroId,
        ModuloId = source.ModuloId,
        Nome = source.Nome,
        Status = source.Status
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
    VALUES (source.Id, source.DataHoraCadastro, source.UsuarioCadastroId, source.ModuloId, source.Nome, source.Status);


GO

DECLARE @ModuloComprasId INT = (SELECT TOP 1 Id FROM dbo.Modulo WHERE Nome = N'Compras');

IF @ModuloComprasId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'Planejamentos')
    BEGIN
        INSERT INTO dbo.Tela (Id, DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
        VALUES (120, SYSUTCDATETIME(), 1, @ModuloComprasId, N'Planejamentos', 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'Desejos')
    BEGIN
        INSERT INTO dbo.Tela (Id, DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
        VALUES (121, SYSUTCDATETIME(), 1, @ModuloComprasId, N'Desejos', 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.Tela WHERE ModuloId = @ModuloComprasId AND Nome = N'Histórico de Produtos')
    BEGIN
        INSERT INTO dbo.Tela (Id, DataHoraCadastro, UsuarioCadastroId, ModuloId, Nome, Status)
        VALUES (122, SYSUTCDATETIME(), 1, @ModuloComprasId, N'Histórico de Produtos', 1);
    END;
END;

SET IDENTITY_INSERT dbo.Tela OFF;

GO
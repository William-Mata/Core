/*
Scripts refatorados por tabela do modulo Seguranca.
Origem: query's/01-seguranca/01-usuario-e-autenticacao.sql
Observacao: organizacao sem alteracao de regra de negocio.
*/

USE [Financeiro];
GO

/* Tabela: Funcionalidade */

IF OBJECT_ID(N'dbo.Funcionalidade', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Funcionalidade
    (
        Id INT IDENTITY(1,1) NOT NULL,
        DataHoraCadastro DATETIME2(0) NOT NULL CONSTRAINT DF_Funcionalidade_DataHoraCadastro DEFAULT (SYSUTCDATETIME()),
        UsuarioCadastroId INT NOT NULL,
        TelaId INT NOT NULL,
        Nome NVARCHAR(100) NOT NULL,
        Status BIT NOT NULL CONSTRAINT DF_Funcionalidade_Status DEFAULT (1),
        CONSTRAINT PK_Funcionalidade PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Funcionalidade_TelaId_Nome UNIQUE (TelaId, Nome)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Funcionalidade_Tela_TelaId')
BEGIN
    ALTER TABLE dbo.Funcionalidade
        WITH CHECK ADD CONSTRAINT FK_Funcionalidade_Tela_TelaId
        FOREIGN KEY (TelaId) REFERENCES dbo.Tela (Id)
        ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Funcionalidade_Usuario_UsuarioCadastroId')
BEGIN
    ALTER TABLE dbo.Funcionalidade
        WITH CHECK ADD CONSTRAINT FK_Funcionalidade_Usuario_UsuarioCadastroId
        FOREIGN KEY (UsuarioCadastroId) REFERENCES dbo.Usuario (Id);
END;
GO

/* SEEDS - Funcionalidade */
SET IDENTITY_INSERT dbo.Funcionalidade ON;

MERGE dbo.Funcionalidade AS target
USING
(
    VALUES
        (1, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Visualizar', 1),
        (2, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 4, N'Criar', 1),
        (3, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Editar', 1),
        (4, CAST('2026-01-01T00:00:00' AS DATETIME2(0)), 1, 2, N'Excluir', 1)
) AS source (Id, DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        DataHoraCadastro = source.DataHoraCadastro,
        UsuarioCadastroId = source.UsuarioCadastroId,
        TelaId = source.TelaId,
        Nome = source.Nome,
        Status = source.Status
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status)
    VALUES (source.Id, source.DataHoraCadastro, source.UsuarioCadastroId, source.TelaId, source.Nome, source.Status);

SET IDENTITY_INSERT dbo.Funcionalidade OFF;
GO

-- Garante conjunto mínimo de funcionalidades de forma idempotente.
;WITH TelasAtivas AS
(
    SELECT t.Id AS TelaId
    FROM dbo.Tela t
    WHERE t.Status = 1
),
TelasCrud AS
(
    SELECT t.Id AS TelaId
    FROM dbo.Tela t
    JOIN dbo.Modulo m ON m.Id = t.ModuloId
    WHERE LOWER(LTRIM(RTRIM(m.Nome))) IN (N'administracao', N'administração', N'financeiro', N'compras')
      AND LOWER(LTRIM(RTRIM(t.Nome))) NOT LIKE N'%documentacao%'
      AND LOWER(LTRIM(RTRIM(t.Nome))) NOT LIKE N'%documentação%'
      AND LOWER(LTRIM(RTRIM(t.Nome))) NOT IN (N'historicoproduto', N'historicoprecos', N'historicodeprodutos', N'histórico de produtos')
),
Acoes AS
(
    SELECT Ordem, Nome
    FROM
    (
        VALUES
            (1, N'Visualizar'),
            (2, N'Criar'),
            (3, N'Editar'),
            (4, N'Excluir')
    ) v(Ordem, Nome)
),
Desejadas AS
(
    SELECT ta.TelaId, 1 AS Ordem, N'Visualizar' AS Nome
    FROM TelasAtivas ta
    UNION ALL
    SELECT tc.TelaId, a.Ordem, a.Nome
    FROM TelasCrud tc
    JOIN Acoes a ON 1 = 1
),
DesejadasNormalizadas AS
(
    SELECT
        d.TelaId,
        d.Ordem,
        d.Nome,
        LOWER(LTRIM(RTRIM(d.Nome))) AS NomeNormalizado
    FROM Desejadas d
),
DesejadasUnicas AS
(
    SELECT
        dn.TelaId,
        dn.Nome,
        dn.NomeNormalizado,
        ROW_NUMBER() OVER
        (
            PARTITION BY dn.TelaId, dn.NomeNormalizado
            ORDER BY dn.Ordem
        ) AS OrdemEscolha
    FROM DesejadasNormalizadas dn
)
MERGE dbo.Funcionalidade AS target
USING
(
    SELECT
        du.TelaId,
        du.Nome,
        du.NomeNormalizado
    FROM DesejadasUnicas du
    WHERE du.OrdemEscolha = 1
) AS source
ON target.TelaId = source.TelaId
   AND LOWER(LTRIM(RTRIM(target.Nome))) = source.NomeNormalizado
WHEN MATCHED THEN
    UPDATE SET
        target.Nome = source.Nome,
        target.Status = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT (DataHoraCadastro, UsuarioCadastroId, TelaId, Nome, Status)
    VALUES (SYSUTCDATETIME(), 1, source.TelaId, source.Nome, 1);

;WITH FuncionalidadesDuplicadas AS
(
    SELECT
        f.Id,
        ROW_NUMBER() OVER
        (
            PARTITION BY f.TelaId, LOWER(LTRIM(RTRIM(f.Nome)))
            ORDER BY CASE WHEN f.Status = 1 THEN 0 ELSE 1 END, f.Id
        ) AS Ordem
    FROM dbo.Funcionalidade f
)
UPDATE f
SET Status = 0
FROM dbo.Funcionalidade f
JOIN FuncionalidadesDuplicadas fd ON fd.Id = f.Id
WHERE fd.Ordem > 1
  AND f.Status = 1;
GO

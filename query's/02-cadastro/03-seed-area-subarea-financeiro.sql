/*
Ordem sugerida: 03
Objetivo: inserir carga inicial de Areas/SubAreas separadas por tipo (Despesa/Receita).
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

DECLARE @UsuarioCadastroId INT = 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Area WHERE Tipo = N'Despesa' AND Nome = N'Alimentacao')
    INSERT INTO dbo.Area (UsuarioCadastroId, Nome, Tipo) VALUES (@UsuarioCadastroId, N'Alimentacao', N'Despesa');

IF NOT EXISTS (SELECT 1 FROM dbo.Area WHERE Tipo = N'Despesa' AND Nome = N'Transporte')
    INSERT INTO dbo.Area (UsuarioCadastroId, Nome, Tipo) VALUES (@UsuarioCadastroId, N'Transporte', N'Despesa');

IF NOT EXISTS (SELECT 1 FROM dbo.Area WHERE Tipo = N'Despesa' AND Nome = N'Moradia')
    INSERT INTO dbo.Area (UsuarioCadastroId, Nome, Tipo) VALUES (@UsuarioCadastroId, N'Moradia', N'Despesa');

IF NOT EXISTS (SELECT 1 FROM dbo.Area WHERE Tipo = N'Receita' AND Nome = N'Salario')
    INSERT INTO dbo.Area (UsuarioCadastroId, Nome, Tipo) VALUES (@UsuarioCadastroId, N'Salario', N'Receita');

IF NOT EXISTS (SELECT 1 FROM dbo.Area WHERE Tipo = N'Receita' AND Nome = N'Investimentos')
    INSERT INTO dbo.Area (UsuarioCadastroId, Nome, Tipo) VALUES (@UsuarioCadastroId, N'Investimentos', N'Receita');

IF NOT EXISTS (SELECT 1 FROM dbo.Area WHERE Tipo = N'Receita' AND Nome = N'Vendas')
    INSERT INTO dbo.Area (UsuarioCadastroId, Nome, Tipo) VALUES (@UsuarioCadastroId, N'Vendas', N'Receita');

;WITH SubAreasSeed AS
(
    SELECT N'Despesa' AS Tipo, N'Alimentacao' AS AreaNome, N'Almoco' AS SubAreaNome UNION ALL
    SELECT N'Despesa', N'Alimentacao', N'Jantar' UNION ALL
    SELECT N'Despesa', N'Alimentacao', N'Supermercado' UNION ALL
    SELECT N'Despesa', N'Transporte', N'Combustivel' UNION ALL
    SELECT N'Despesa', N'Transporte', N'Aplicativo' UNION ALL
    SELECT N'Despesa', N'Transporte', N'Manutencao' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Aluguel' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Condominio' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Energia' UNION ALL
    SELECT N'Receita', N'Salario', N'Holerite' UNION ALL
    SELECT N'Receita', N'Salario', N'Decimo Terceiro' UNION ALL
    SELECT N'Receita', N'Salario', N'Ferias' UNION ALL
    SELECT N'Receita', N'Investimentos', N'Dividendos' UNION ALL
    SELECT N'Receita', N'Investimentos', N'Juros' UNION ALL
    SELECT N'Receita', N'Investimentos', N'Rendimento' UNION ALL
    SELECT N'Receita', N'Vendas', N'Produto' UNION ALL
    SELECT N'Receita', N'Vendas', N'Servico' UNION ALL
    SELECT N'Receita', N'Vendas', N'Comissao'
)
INSERT INTO dbo.SubArea (UsuarioCadastroId, AreaId, Nome)
SELECT
    @UsuarioCadastroId,
    a.Id,
    s.SubAreaNome
FROM SubAreasSeed s
INNER JOIN dbo.Area a
    ON a.Nome = s.AreaNome
    AND a.Tipo = s.Tipo
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.SubArea sa
    WHERE sa.AreaId = a.Id
      AND sa.Nome = s.SubAreaNome
);
GO

/*
Ordem sugerida: 03
Objetivo: inserir carga inicial de Areas/SubAreas separadas por tipo (Despesa/Receita).
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

DECLARE @UsuarioCadastroId INT = 1;

;WITH AreasSeed AS
(
    SELECT N'Despesa' AS Tipo, N'Alimentacao' AS Nome UNION ALL
    SELECT N'Despesa', N'Transporte' UNION ALL
    SELECT N'Despesa', N'Moradia' UNION ALL
    SELECT N'Despesa', N'Saude' UNION ALL
    SELECT N'Despesa', N'Educacao' UNION ALL
    SELECT N'Despesa', N'Lazer' UNION ALL
    SELECT N'Despesa', N'Servicos' UNION ALL
    SELECT N'Despesa', N'Impostos' UNION ALL
    SELECT N'Despesa', N'Seguros' UNION ALL
    SELECT N'Despesa', N'Assinaturas' UNION ALL
    SELECT N'Despesa', N'Vestuario' UNION ALL
    SELECT N'Despesa', N'Manutencao' UNION ALL
    SELECT N'Despesa', N'Viagens' UNION ALL
    SELECT N'Despesa', N'Animais' UNION ALL
    SELECT N'Despesa', N'Tecnologia' UNION ALL
    SELECT N'Despesa', N'Investimentos' UNION ALL
    SELECT N'Despesa', N'Despesas Bancarias' UNION ALL
    SELECT N'Despesa', N'Outras Despesas' UNION ALL
    SELECT N'Receita', N'Salario' UNION ALL
    SELECT N'Receita', N'Freelance' UNION ALL
    SELECT N'Receita', N'Vendas' UNION ALL
    SELECT N'Receita', N'Investimentos' UNION ALL
    SELECT N'Receita', N'Alugueis' UNION ALL
    SELECT N'Receita', N'Reembolsos' UNION ALL
    SELECT N'Receita', N'Beneficios' UNION ALL
    SELECT N'Receita', N'Bonificacoes' UNION ALL
    SELECT N'Receita', N'Rendas Extras' UNION ALL
    SELECT N'Receita', N'Premios' UNION ALL
    SELECT N'Receita', N'Receitas Financeiras' UNION ALL
    SELECT N'Receita', N'Outras Receitas'
)
INSERT INTO dbo.Area (UsuarioCadastroId, Nome, Tipo)
SELECT @UsuarioCadastroId, s.Nome, s.Tipo
FROM AreasSeed s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Area a
    WHERE a.Tipo = s.Tipo
      AND a.Nome = s.Nome
);

;WITH SubAreasSeed AS
(
    SELECT N'Despesa' AS Tipo, N'Alimentacao' AS AreaNome, N'Almoco' AS SubAreaNome UNION ALL
    SELECT N'Despesa', N'Alimentacao', N'Jantar' UNION ALL
    SELECT N'Despesa', N'Alimentacao', N'Supermercado' UNION ALL
    SELECT N'Despesa', N'Alimentacao', N'Cafeteria' UNION ALL
    SELECT N'Despesa', N'Alimentacao', N'Padaria' UNION ALL
    SELECT N'Despesa', N'Transporte', N'Combustivel' UNION ALL
    SELECT N'Despesa', N'Transporte', N'Aplicativo' UNION ALL
    SELECT N'Despesa', N'Transporte', N'Transporte Publico' UNION ALL
    SELECT N'Despesa', N'Transporte', N'Pedagio' UNION ALL
    SELECT N'Despesa', N'Transporte', N'Estacionamento' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Aluguel' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Condominio' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Energia' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Agua' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Internet' UNION ALL
    SELECT N'Despesa', N'Moradia', N'Gas' UNION ALL
    SELECT N'Despesa', N'Saude', N'Plano de Saude' UNION ALL
    SELECT N'Despesa', N'Saude', N'Consultas' UNION ALL
    SELECT N'Despesa', N'Saude', N'Exames' UNION ALL
    SELECT N'Despesa', N'Saude', N'Farmacia' UNION ALL
    SELECT N'Despesa', N'Educacao', N'Escola' UNION ALL
    SELECT N'Despesa', N'Educacao', N'Faculdade' UNION ALL
    SELECT N'Despesa', N'Educacao', N'Cursos' UNION ALL
    SELECT N'Despesa', N'Educacao', N'Livros' UNION ALL
    SELECT N'Despesa', N'Lazer', N'Cinema' UNION ALL
    SELECT N'Despesa', N'Lazer', N'Restaurantes' UNION ALL
    SELECT N'Despesa', N'Lazer', N'Viagens de Lazer' UNION ALL
    SELECT N'Despesa', N'Lazer', N'Eventos' UNION ALL
    SELECT N'Despesa', N'Servicos', N'Contabilidade' UNION ALL
    SELECT N'Despesa', N'Servicos', N'Advocacia' UNION ALL
    SELECT N'Despesa', N'Servicos', N'Consultoria' UNION ALL
    SELECT N'Despesa', N'Servicos', N'Mao de Obra' UNION ALL
    SELECT N'Despesa', N'Impostos', N'IRPF' UNION ALL
    SELECT N'Despesa', N'Impostos', N'IPTU' UNION ALL
    SELECT N'Despesa', N'Impostos', N'IPVA' UNION ALL
    SELECT N'Despesa', N'Impostos', N'Taxas Municipais' UNION ALL
    SELECT N'Despesa', N'Seguros', N'Seguro Auto' UNION ALL
    SELECT N'Despesa', N'Seguros', N'Seguro Residencial' UNION ALL
    SELECT N'Despesa', N'Seguros', N'Seguro Vida' UNION ALL
    SELECT N'Despesa', N'Assinaturas', N'Streaming' UNION ALL
    SELECT N'Despesa', N'Assinaturas', N'Software' UNION ALL
    SELECT N'Despesa', N'Assinaturas', N'Academia' UNION ALL
    SELECT N'Despesa', N'Vestuario', N'Roupas' UNION ALL
    SELECT N'Despesa', N'Vestuario', N'Calcados' UNION ALL
    SELECT N'Despesa', N'Manutencao', N'Reforma' UNION ALL
    SELECT N'Despesa', N'Manutencao', N'Manutencao Veiculo' UNION ALL
    SELECT N'Despesa', N'Manutencao', N'Manutencao Equipamentos' UNION ALL
    SELECT N'Despesa', N'Viagens', N'Passagens' UNION ALL
    SELECT N'Despesa', N'Viagens', N'Hospedagem' UNION ALL
    SELECT N'Despesa', N'Viagens', N'Alimentacao em Viagem' UNION ALL
    SELECT N'Despesa', N'Animais', N'Racao' UNION ALL
    SELECT N'Despesa', N'Animais', N'Veterinario' UNION ALL
    SELECT N'Despesa', N'Animais', N'Banho e Tosa' UNION ALL
    SELECT N'Despesa', N'Tecnologia', N'Equipamentos' UNION ALL
    SELECT N'Despesa', N'Tecnologia', N'Perifericos' UNION ALL
    SELECT N'Despesa', N'Tecnologia', N'Aplicativos' UNION ALL
    SELECT N'Despesa', N'Investimentos', N'Aportes' UNION ALL
    SELECT N'Despesa', N'Investimentos', N'Taxas de Corretagem' UNION ALL
    SELECT N'Despesa', N'Despesas Bancarias', N'Tarifas' UNION ALL
    SELECT N'Despesa', N'Despesas Bancarias', N'Anuidade Cartao' UNION ALL
    SELECT N'Despesa', N'Outras Despesas', N'Diversos' UNION ALL
    SELECT N'Despesa', N'Outras Despesas', N'Imprevistos' UNION ALL
    SELECT N'Receita', N'Salario', N'Holerite' UNION ALL
    SELECT N'Receita', N'Salario', N'Decimo Terceiro' UNION ALL
    SELECT N'Receita', N'Salario', N'Ferias' UNION ALL
    SELECT N'Receita', N'Salario', N'Participacao nos Lucros' UNION ALL
    SELECT N'Receita', N'Freelance', N'Desenvolvimento' UNION ALL
    SELECT N'Receita', N'Freelance', N'Design' UNION ALL
    SELECT N'Receita', N'Freelance', N'Consultoria' UNION ALL
    SELECT N'Receita', N'Investimentos', N'Dividendos' UNION ALL
    SELECT N'Receita', N'Investimentos', N'Juros' UNION ALL
    SELECT N'Receita', N'Investimentos', N'Rendimento' UNION ALL
    SELECT N'Receita', N'Vendas', N'Produto' UNION ALL
    SELECT N'Receita', N'Vendas', N'Servico' UNION ALL
    SELECT N'Receita', N'Vendas', N'Comissao' UNION ALL
    SELECT N'Receita', N'Alugueis', N'Aluguel Residencial' UNION ALL
    SELECT N'Receita', N'Alugueis', N'Aluguel Comercial' UNION ALL
    SELECT N'Receita', N'Reembolsos', N'Reembolso de Despesa' UNION ALL
    SELECT N'Receita', N'Reembolsos', N'Reembolso Corporativo' UNION ALL
    SELECT N'Receita', N'Beneficios', N'Vale Alimentacao' UNION ALL
    SELECT N'Receita', N'Beneficios', N'Vale Transporte' UNION ALL
    SELECT N'Receita', N'Bonificacoes', N'Bonus' UNION ALL
    SELECT N'Receita', N'Bonificacoes', N'Premiacao' UNION ALL
    SELECT N'Receita', N'Rendas Extras', N'Cashback' UNION ALL
    SELECT N'Receita', N'Rendas Extras', N'Afiliados' UNION ALL
    SELECT N'Receita', N'Premios', N'Sorteio' UNION ALL
    SELECT N'Receita', N'Premios', N'Concurso' UNION ALL
    SELECT N'Receita', N'Receitas Financeiras', N'Juros de Conta' UNION ALL
    SELECT N'Receita', N'Receitas Financeiras', N'Resgate de Investimento' UNION ALL
    SELECT N'Receita', N'Outras Receitas', N'Diversos' UNION ALL
    SELECT N'Receita', N'Outras Receitas', N'Imprevistos'
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

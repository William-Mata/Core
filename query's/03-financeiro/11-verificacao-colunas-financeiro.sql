/*
Ordem sugerida: 11
Objetivo: varredura de colunas esperadas no modulo financeiro.
Banco alvo: Financeiro
*/

USE [Financeiro];
GO

;WITH Esperadas AS
(
    SELECT N'dbo.Despesa' AS Tabela, N'DataEfetivacao' AS Coluna UNION ALL
    SELECT N'dbo.Despesa', N'Recorrencia' UNION ALL
    SELECT N'dbo.Despesa', N'RecorrenciaFixa' UNION ALL
    SELECT N'dbo.Despesa', N'QuantidadeRecorrencia' UNION ALL
    SELECT N'dbo.Despesa', N'ValorTotalRateioAmigos' UNION ALL
    SELECT N'dbo.Despesa', N'TipoRateioAmigos' UNION ALL
    SELECT N'dbo.Despesa', N'DespesaRecorrenciaOrigemId' UNION ALL
    SELECT N'dbo.Despesa', N'ValorEfetivacao' UNION ALL
    SELECT N'dbo.DespesaAmigoRateio', N'Valor' UNION ALL
    SELECT N'dbo.DespesaAreaRateio', N'Valor' UNION ALL
    SELECT N'dbo.Receita', N'DataEfetivacao' UNION ALL
    SELECT N'dbo.Receita', N'Recorrencia' UNION ALL
    SELECT N'dbo.Receita', N'RecorrenciaFixa' UNION ALL
    SELECT N'dbo.Receita', N'QuantidadeRecorrencia' UNION ALL
    SELECT N'dbo.Receita', N'ValorTotalRateioAmigos' UNION ALL
    SELECT N'dbo.Receita', N'TipoRateioAmigos' UNION ALL
    SELECT N'dbo.Receita', N'ValorEfetivacao' UNION ALL
    SELECT N'dbo.Receita', N'ContaBancariaId' UNION ALL
    SELECT N'dbo.ReceitaAmigoRateio', N'Valor' UNION ALL
    SELECT N'dbo.ReceitaAreaRateio', N'Valor' UNION ALL
    SELECT N'dbo.HistoricoTransacaoFinanceira', N'TipoPagamento' UNION ALL
    SELECT N'dbo.HistoricoTransacaoFinanceira', N'TipoRecebimento' UNION ALL
    SELECT N'dbo.Reembolso', N'DataLancamento' UNION ALL
    SELECT N'dbo.Reembolso', N'DataEfetivacao' UNION ALL
    SELECT N'dbo.Documento', N'NomeArquivo' UNION ALL
    SELECT N'dbo.Documento', N'CaminhoArquivo' UNION ALL
    SELECT N'dbo.Documento', N'DespesaId' UNION ALL
    SELECT N'dbo.Documento', N'ReceitaId' UNION ALL
    SELECT N'dbo.Documento', N'ReembolsoId'
)
SELECT
    e.Tabela,
    e.Coluna,
    CASE WHEN t.object_id IS NULL THEN N'TABELA_AUSENTE'
         WHEN c.column_id IS NULL THEN N'COLUNA_AUSENTE'
         ELSE N'OK'
    END AS Status
FROM Esperadas e
LEFT JOIN sys.tables t
    ON t.object_id = OBJECT_ID(e.Tabela)
LEFT JOIN sys.columns c
    ON c.object_id = t.object_id
   AND c.name = e.Coluna
ORDER BY e.Tabela, e.Coluna;
GO

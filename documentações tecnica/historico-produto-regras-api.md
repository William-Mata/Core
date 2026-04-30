# HistoricoPrecoCompraController - Regras de API

## Objetivo
Documentar o contrato de consulta do historico de preco por `Produto` no modulo Compras.

## Autenticacao
- Todas as rotas exigem JWT Bearer.

## Rota
- `GET /api/compras/historico-precos`

## Permissoes de funcionalidade (tela)
- Tela: `Histórico de Produtos` (historico de produto no modulo `Compras`)
- Funcionalidade ativa:
  - `Visualizar`
- Regra de excecao:
  - Esta tela nao recebe `Criar`, `Editar` e `Excluir`.

## Query params
- `descricao` (opcional)
- `unidade` (opcional, enum `UnidadeMedidaCompra`)
- `dataInicio` (opcional)
- `dataFim` (opcional)

## Regras globais
- O retorno e sempre filtrado pelo usuario autenticado.
- Considera historico criado em:
  - itens proprios
  - itens de listas compartilhadas com o usuario
- Consolidacao por `ProdutoId + Unidade`.
- Campos agregados por grupo:
  - ultimo preco
  - menor preco
  - maior preco
  - media de preco
  - data do ultimo preco
  - total de ocorrencias
- Cada grupo tambem retorna `historicoPrecos` com a serie cronologica detalhada.
- `historicoPrecos`:
  - ordenado por data crescente
  - ignora registros com preco invalido (`<= 0`)
  - ultimo item da serie e a fonte de `ultimoPreco` e `dataUltimoPreco`

## Regras de preenchimento do historico
- `HistoricoProduto` e alimentado por alteracoes relevantes de item com preco valido.
- Origem pode ser:
  - `Estimado`
  - `Confirmado`
- Edicoes sem preco valido (`null` ou `<= 0`) nao geram novo historico.

## Exemplo de response
```json
[
  {
    "produtoId": 9,
    "descricao": "Tomate",
    "unidade": "Kg",
    "ultimoPreco": 12.30,
    "menorPreco": 8.99,
    "maiorPreco": 12.30,
    "mediaPreco": 10.44,
    "dataUltimoPreco": "2026-04-20T15:31:00Z",
    "totalOcorrencias": 5,
    "historicoPrecos": [
      {
        "data": "2026-03-15",
        "valor": 8.99
      },
      {
        "data": "2026-04-01",
        "valor": 10.90
      },
      {
        "data": "2026-04-20",
        "valor": 12.30
      }
    ]
  }
]
```

## Erros comuns
- `dados_invalidos`
- `usuario_nao_autenticado`

## Rastreabilidade
- Controller: `Core.Api/Controllers/Compras/HistoricoPrecoCompraController.cs`
- Service: `Core.Application/Services/Compras/ComprasService.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Compras/ComprasRepository.cs`
- Entidades: `Core.Domain/Entities/Compras/HistoricoProduto.cs` e `Core.Domain/Entities/Compras/Produto.cs`

# HistoricoPrecoCompraController - Regras de API

## Objetivo
Documentar o contrato de consulta do historico de preco por `Produto` no modulo Compras.

## Autenticacao
- Todas as rotas exigem JWT Bearer.

## Rota
- `GET /api/compras/historico-precos`
- `GET /api/compras/historico-precos/dashboard/variacao`
- `GET /api/compras/historico-precos/dashboard/economia-potencial`

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

## Endpoints de dashboard

### GET /api/compras/historico-precos/dashboard/variacao
- Objetivo: retornar produtos com maior variacao de preco para o dashboard de Compras.
- Autorizacao:
  - JWT Bearer obrigatorio.
  - historico filtrado pelo usuario autenticado ou por listas compartilhadas com participacao ativa.
- Request:
  - query opcional: `limite` (int, default `10`, maximo aplicado pelo service `100`).
- Response:
```json
[
  {
    "id": "301-unidade",
    "produto": "Cafe 500g",
    "ultimoPreco": 21.9,
    "menorPreco": 17.5,
    "maiorPreco": 24.9,
    "mediaPreco": 20.8,
    "percentualVariacao": 42.29,
    "potencialEconomiaUnitaria": 4.4
  }
]
```
- Regras:
  - baseado em `HistoricoProduto`.
  - considera somente precos validos (`PrecoUnitario > 0`).
  - considera produtos com historico suficiente (`TotalOcorrencias >= 2`).
  - consolida por `ProdutoId + Unidade`.
  - `ultimoPreco` vem do historico mais recente do grupo.
  - `percentualVariacao` usa `(maiorPreco - menorPreco) / menorPreco * 100`.
  - `potencialEconomiaUnitaria` usa diferenca positiva entre `ultimoPreco` e `menorPreco`.
  - ordena por maior economia potencial unitaria.
  - usa `AsNoTracking()` e projecao agregada.
- Efeitos colaterais:
  - sem escrita.

### GET /api/compras/historico-precos/dashboard/economia-potencial
- Objetivo: retornar economia potencial total e produtos com maior economia.
- Autorizacao:
  - JWT Bearer obrigatorio.
- Request:
  - query opcional: `limite` (int, default `10`, maximo aplicado pelo service `100`).
- Response:
```json
{
  "economiaPotencialTotal": 92.3,
  "produtosComMelhorEconomia": [
    {
      "id": "301-unidade",
      "produto": "Cafe 500g",
      "economiaUnitaria": 4.4,
      "ultimoPreco": 21.9,
      "menorPreco": 17.5
    }
  ]
}
```
- Regras:
  - reutiliza a mesma base agregada performatica de variacao de precos.
  - considera somente diferencas positivas.
  - `economiaPotencialTotal` soma a economia unitaria dos produtos retornados pelo limite.
  - produtos ordenados por maior economia.
- Efeitos colaterais:
  - sem escrita.

## Erros comuns
- `dados_invalidos`
- `usuario_nao_autenticado`

## Rastreabilidade
- Controller: `Core.Api/Controllers/Compras/HistoricoPrecoCompraController.cs`
- Service: `Core.Application/Services/Compras/ComprasService.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Compras/ComprasRepository.cs`
- Entidades: `Core.Domain/Entities/Compras/HistoricoProduto.cs` e `Core.Domain/Entities/Compras/Produto.cs`
- DTOs de dashboard: `Core.Application/DTOs/Compras/ComprasDtos.cs`
- Read models de dashboard: `Core.Domain/Interfaces/Compras/ComprasDashboardReadModels.cs`

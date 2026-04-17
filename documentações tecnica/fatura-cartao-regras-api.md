# Fatura Cartao - Regras de API

## 1. Contexto

- Modulo: Financeiro
- Controller: `FaturaCartaoController`
- Escopo desta documentacao: listagem de faturas e detalhamento com lancamentos vinculados para exibição no front.

## 2. Contrato de consumo

### 2.1 GET /api/financeiro/faturas-cartao (Listagem basica)

- Metodo HTTP: `GET`
- Rota: `/api/financeiro/faturas-cartao`
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado

#### Query params

- `competencia` (obrigatorio): mes/ano da consulta. Exemplos aceitos: `2026-04`, `04/2026`.
- `cartaoId` (opcional): filtra por um cartao especifico.

#### Response de sucesso

- Status code: `200 OK`
- Body: array de `FaturaCartaoListaDto`

```json
[
  {
    "id": 10,
    "cartaoId": 3,
    "competencia": "2026-04",
    "valorTotal": 1580.40,
    "status": "fechada",
    "dataFechamento": "2026-04-10",
    "dataEfetivacao": null,
    "dataEstorno": null
  }
]
```

### 2.2 GET /api/financeiro/faturas-cartao/detalhes (Fatura + lancamentos)

- Metodo HTTP: `GET`
- Rota: `/api/financeiro/faturas-cartao/detalhes`
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado

#### Query params

- `competencia` (obrigatorio): mes/ano da consulta.
- `tipoTransacao` (opcional): `despesa`, `receita` ou `reembolso`.

Regra do retorno:
- o endpoint sempre retorna uma lista de faturas da competencia informada (nao filtra por cartao);
- quando `tipoTransacao` for `null`/ausente, retorna todos os tipos vinculados a cada fatura;
- quando informado, retorna apenas os lancamentos do tipo enviado.

#### Response de sucesso

- Status code: `200 OK`
- Body: array de `FaturaCartaoDetalheDto`

```json
[
  {
    "id": 10,
    "cartaoId": 3,
    "competencia": "2026-04",
    "valorTotal": 1580.40,
    "valorTotalTransacoes": 1200.40,
    "status": "fechada",
    "dataFechamento": "2026-04-10",
    "dataEfetivacao": null,
    "dataEstorno": null,
    "lancamentos": [
      {
        "tipoTransacao": "despesa",
        "transacaoId": 981,
        "descricao": "Supermercado",
        "competencia": "2026-04",
        "dataLancamento": "2026-04-03",
        "dataEfetivacao": "2026-04-03",
        "valor": 350.25,
        "status": "efetivada"
      }
    ]
  }
]
```

Observacao sobre valores:
- `valorTotal`: total consolidado da fatura (todos os lancamentos vinculados).
- `valorTotalTransacoes`: soma apenas das transacoes efetivamente retornadas no detalhe (respeita o filtro `tipoTransacao`).

## 3. Regras aplicadas

### 3.1 Validacoes de entrada

Fatos confirmados:
- `competencia` e obrigatoria nos dois endpoints de consulta de fatura.
- `tipoTransacao`, quando informado, deve ser apenas `despesa`, `receita` ou `reembolso`.

### 3.2 Regras de negocio

Fatos confirmados:
- a listagem basica e o detalhe filtram sempre por usuario autenticado.
- no endpoint de detalhes, a API publica uma solicitacao para rotina de garantia e saneamento em background (padrao assíncrono por fila):
  - considera apenas cartoes de credito;
  - garante fatura da competencia consultada e das 3 proximas competencias (cria somente se nao existir);
  - nao gera faturas fora desse intervalo (competencia atual + 3 proximas);
  - localiza transacoes de cartao da competencia consultada sem `FaturaCartaoId` e vincula na fatura correta;
  - nao altera transacoes ja vinculadas;
  - nao vincula transacoes de outra competencia.
- no detalhe, quando `tipoTransacao` nao for informado, a API considera todos os tipos vinculados.
- o fechamento automatico da fatura pode ser aplicado na consulta conforme regra de fechamento do cartao.
- a fatura muda para `fechada` quando a data atual atingir `dataVencimento - 7 dias`.
- `dataVencimento` da fatura e calculada com base no `diaVencimento` do cartao para a competencia consultada e ajustada para o proximo dia util quando cair em fim de semana.
- `dataFechamento` e calculada a partir de `dataVencimento - 7 dias` e ajustada para o dia util anterior quando cair em fim de semana.
- o total persistido da fatura e recalculado no fluxo de consulta para manter consistencia.
- a rotina e idempotente: reprocessar a mesma competencia nao duplica faturas nem religa itens ja vinculados.
- por ser background, o detalhamento pode refletir o saneamento de forma eventual.

### 3.3 Efeitos colaterais

Fatos confirmados:
- persistencia:
  - consulta pode atualizar `Status`/`DataFechamento` da fatura (fechamento automatico);
  - consulta pode atualizar `ValorTotal` da fatura quando identificar divergencia.
- integracoes/eventos:
  - nao ha publicacao de evento externo nesses endpoints.

## 4. Erros e cenarios de falha

| Status | Condicao | Mensagem/retorno |
|---|---|---|
| 400 | `competencia` ausente ou vazia | `code: "competencia_obrigatoria"` |
| 400 | `tipoTransacao` invalido | `code: "tipo_transacao_invalido"` |
| 401 | Token ausente/invalido | ProblemDetails de nao autorizado |
| 500 | Erro nao tratado | `code: "erro_interno"` |

## 5. Exemplos de consumo

### 5.1 Listagem basica

```bash
curl -X GET "https://api.exemplo.com/api/financeiro/faturas-cartao?competencia=2026-04&cartaoId=3" \
  -H "Authorization: Bearer <token>"
```

### 5.2 Detalhamento sem filtro de tipo (retorna todos)

```bash
curl -X GET "https://api.exemplo.com/api/financeiro/faturas-cartao/detalhes?competencia=2026-04" \
  -H "Authorization: Bearer <token>"
```

### 5.3 Detalhamento filtrando apenas despesas

```bash
curl -X GET "https://api.exemplo.com/api/financeiro/faturas-cartao/detalhes?competencia=2026-04&tipoTransacao=despesa" \
  -H "Authorization: Bearer <token>"
```

## 6. Rastreabilidade no codigo

- Controller/rota: `Core.Api/Controllers/Financeiro/FaturaCartaoController.cs`
- Service/use case: `Core.Application/Services/Financeiro/FaturaCartaoService.cs`
- DTOs: `Core.Application/DTOs/Financeiro/FaturaCartaoDtos.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/FaturaCartaoRepository.cs`
- Excecoes/enums: `DomainException`, `NotFoundException`, `StatusFaturaCartao`
- Tratamento de erro HTTP: `Core.Api/Middlewares/ErrorHandlingMiddleware.cs` e `Core.Api/Extensions/ErroMensagemExtensions.cs`

## 7. Scripts de banco relacionados

- `query's/03-financeiro/20-fatura-cartao.sql`
- `query's/00-script-mestre.sql` (sessao Ordem 20 - Fatura de cartao)

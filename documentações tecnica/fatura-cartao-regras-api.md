# Fatura Cartao - Regras de API

## 1. Contexto

- Modulo: Financeiro
- Controller: `FaturaCartaoController`
- Escopo desta documentacao: listagem de faturas, detalhamento e fluxo de efetivacao/estorno de fatura com impacto financeiro.

## 2. Contrato de consumo

### 2.0 Formato de datas dos lancamentos

- `dataLancamento` e `dataEfetivacao` dos itens de `lancamentos` sao `DateTime` em ISO 8601 no formato `yyyy-MM-ddTHH:mm:ss`.
- Datas da fatura (`dataVencimento`, `dataFechamento`, `dataEstorno`) permanecem `yyyy-MM-dd`.

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
    "dataVencimento": "2026-04-17",
    "valorTotal": 1580.40,
    "status": "vencida",
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
    "dataVencimento": "2026-04-17",
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
        "dataLancamento": "2026-04-03T09:15:00",
        "dataEfetivacao": "2026-04-03T09:15:00",
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

### 2.3 POST /api/financeiro/faturas-cartao/{id}/efetivar

- Metodo HTTP: `POST`
- Rota: `/api/financeiro/faturas-cartao/{id}/efetivar`
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado

#### Request body

```json
{
  "dataEfetivacao": "2026-04-19T10:00:00",
  "contaBancariaId": 10,
  "valorTotal": 1200.00,
  "valorEfetivacao": 1000.00,
  "observacaoHistorico": "Pagamento parcial da fatura"
}
```

#### Regras de payload

- `dataEfetivacao` obrigatoria.
- `contaBancariaId` obrigatoria e deve pertencer ao usuario autenticado.
- `valorTotal` obrigatorio e deve ser exatamente igual ao total atual da fatura (campo bloqueado no front).
- `valorEfetivacao` obrigatorio e menor ou igual a `valorTotal`.
- excecao: quando `valorTotal` da fatura for `0`, a efetivacao nao gera despesa de pagamento nem historico financeiro; apenas altera o status da fatura para `efetivada`.

#### Response de sucesso

- Status code: `200 OK`
- Body: `FaturaCartaoListaDto` atualizado.

### 2.4 POST /api/financeiro/faturas-cartao/{id}/estornar

- Metodo HTTP: `POST`
- Rota: `/api/financeiro/faturas-cartao/{id}/estornar`
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado

#### Request body

```json
{
  "dataEstorno": "2026-04-20T10:45:00",
  "observacaoHistorico": "Ajuste do pagamento",
  "ocultarDoHistorico": true
}
```

#### Regras de payload

- `dataEstorno` obrigatoria com data/hora (ISO 8601).
- `ocultarDoHistorico` opcional (default `true`) para ocultar os historicos anteriores da transacao.

#### Response de sucesso

- Status code: `200 OK`
- Body: `FaturaCartaoListaDto` atualizado.

## 3. Regras aplicadas

### 3.1 Validacoes de entrada

Fatos confirmados:
- `competencia` e obrigatoria nos dois endpoints de consulta de fatura.
- `tipoTransacao`, quando informado, deve ser apenas `despesa`, `receita` ou `reembolso`.
- na efetivacao de fatura:
  - `dataEfetivacao` e obrigatoria;
  - `contaBancariaId` e obrigatoria e valida para o usuario;
  - `valorTotal` deve casar com `ValorTotal` atual da fatura;
  - `valorEfetivacao` deve ser `> 0` e `<= valorTotal`.
- no estorno de fatura:
  - `dataEstorno` e obrigatoria com data/hora;
  - `dataEstorno` nao pode ser menor que `dataEfetivacao` da fatura.
- a efetivacao da fatura so e permitida quando todas as transacoes vinculadas estiverem efetivadas:
  - despesa: `Efetivada` (ou `Cancelada` para itens desconsiderados);
  - receita: `Efetivada` (ou `Cancelada` para itens desconsiderados);
  - reembolso: `Pago` (ou `Cancelado` para itens desconsiderados).

### 3.2 Regras de negocio

Fatos confirmados:
- a listagem basica e o detalhe filtram sempre por usuario autenticado.
- no endpoint de detalhes, a API publica uma solicitacao para rotina de garantia e saneamento em background (padrao assincrono por fila):
  - considera apenas cartoes de credito;
  - garante fatura da competencia consultada e das 3 proximas competencias (cria somente se nao existir);
  - nao gera faturas fora desse intervalo (competencia atual + 3 proximas);
  - localiza transacoes de cartao da competencia consultada sem `FaturaCartaoId` e vincula na fatura correta;
  - nao altera transacoes ja vinculadas;
  - nao vincula transacoes de outra competencia.
- no detalhe, quando `tipoTransacao` nao for informado, a API considera todos os tipos vinculados.
- o fechamento automatico da fatura pode ser aplicado na consulta conforme regra de fechamento do cartao.
- a fatura muda para `fechada` quando a data atual atingir `dataVencimento - 7 dias`.
- no cadastro da fatura, `dataVencimento` e persistida no registro da fatura e ajustada para o proximo dia util quando cair em fim de semana.
- `dataVencimento` da fatura e calculada com base no `diaVencimento` do cartao para a competencia consultada e ajustada para o proximo dia util quando cair em fim de semana.
- `dataFechamento` e calculada a partir de `dataVencimento - 7 dias` e ajustada para o dia util anterior quando cair em fim de semana.
- despesa, receita e reembolso vinculados em fatura recebem `dataVencimento` igual ao `dataVencimento` da fatura vinculada.
- o total persistido da fatura e recalculado no fluxo de consulta para manter consistencia.
- a rotina e idempotente: reprocessar a mesma competencia nao duplica faturas nem religa itens ja vinculados.
- por ser background, o detalhamento pode refletir o saneamento de forma eventual.
- a efetivacao de fatura so e permitida para status: `aberta`, `fechada`, `vencida` e `estornada`.
- quando a data atual ultrapassa `dataVencimento` e a fatura ainda nao foi efetivada, o status automatico passa para `vencida`.
- ao efetivar:
  - a fatura passa para `efetivada`;
  - e gerada/atualizada uma despesa de pagamento de fatura;
  - o valor efetivado consome saldo da conta bancaria informada;
  - o valor efetivado retorna para o limite (saldo disponivel) do cartao.
- quando `valorTotal` da fatura e `0`:
  - nao gera despesa de pagamento;
  - nao altera saldo da conta nem limite do cartao;
  - apenas atualiza status/data de efetivacao da fatura.
- ao estornar:
  - a fatura passa para `estornada`;
  - a despesa de pagamento da fatura volta para `pendente`;
  - o saldo da conta bancaria e recomposto;
  - o limite do cartao e consumido novamente (comportamento oposto da efetivacao).
- quando uma transacao vinculada a fatura (despesa/receita/reembolso) e estornada, a API garante o estorno da fatura antes de concluir o estorno da transacao.

### 3.3 Efeitos colaterais

Fatos confirmados:
- persistencia:
  - consulta pode atualizar `Status`/`DataFechamento` da fatura (fechamento automatico);
  - consulta pode atualizar `ValorTotal` da fatura quando identificar divergencia.
- efetivacao/estorno:
  - cria/atualiza despesa de pagamento vinculada ao fluxo da fatura;
  - grava historico financeiro de despesa (conta bancaria) e receita (cartao) para manter saldo e limite coerentes;
  - no estorno, pode ocultar historicos anteriores quando `ocultarDoHistorico=true`.
- integracoes/eventos:
  - nao ha publicacao de evento externo nesses endpoints.

## 4. Erros e cenarios de falha

| Status | Condicao | Mensagem/retorno |
|---|---|---|
| 400 | `competencia` ausente ou vazia | `code: "competencia_obrigatoria"` |
| 400 | `tipoTransacao` invalido | `code: "tipo_transacao_invalido"` |
| 400 | status da fatura invalido para efetivar/estornar | `code: "status_invalido"` |
| 400 | conta bancaria invalida na efetivacao | `code: "conta_bancaria_invalida"` |
| 400 | payload invalido na efetivacao | `code: "data_efetivacao_obrigatoria"`, `code: "conta_bancaria_obrigatoria"`, `code: "valor_total_invalido"`, `code: "valor_efetivacao_invalido"` |
| 400 | existe transacao da fatura sem efetivacao | `code: "fatura_transacoes_pendentes"` |
| 400 | payload invalido no estorno | `code: "data_estorno_obrigatoria"`, `code: "periodo_invalido"` |
| 400 | despesa de pagamento nao encontrada para estorno | `code: "despesa_pagamento_fatura_nao_encontrada"` |
| 404 | fatura nao encontrada | `code: "fatura_nao_encontrada"` |
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

### 5.4 Efetivar fatura

```bash
curl -X POST "https://api.exemplo.com/api/financeiro/faturas-cartao/10/efetivar" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "dataEfetivacao": "2026-04-19T10:00:00",
    "contaBancariaId": 10,
    "valorTotal": 1200.00,
    "valorEfetivacao": 1000.00
  }'
```

### 5.5 Estornar fatura

```bash
curl -X POST "https://api.exemplo.com/api/financeiro/faturas-cartao/10/estornar" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "dataEstorno": "2026-04-20",
    "ocultarDoHistorico": true
  }'
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
- `query's/03-financeiro/23-fix-fatura-cartao-efetivacao-estorno.sql`
- `query's/03-financeiro/24-fix-estorno-datetime2.sql`
- `query's/00-script-mestre.sql` (sessao Ordem 20 - Fatura de cartao)

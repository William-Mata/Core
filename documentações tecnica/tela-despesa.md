# Tela de Despesa

## Objetivo
Documentar o contrato atual da API para a tela de despesa, com foco nas regras reais aplicadas no backend.

Arquivos fonte usados:
- `Core.Api/Controllers/Financeiro/DespesaController.cs`
- `Core.Application/Services/Financeiro/DespesaService.cs`
- `Core.Application/DTOs/Financeiro/DespesaDtos.cs`
- `Core.Domain/Enums/Recorrencia.cs`
- `Core.Domain/Enums/StatusDespesa.cs`

## Autenticacao
Todos os endpoints exigem autenticacao (`[Authorize]`).

## Endpoints
- `GET /api/financeiro/despesas`
- `GET /api/financeiro/despesas/{id}`
- `POST /api/financeiro/despesas`
- `PUT /api/financeiro/despesas/{id}`
- `POST /api/financeiro/despesas/{id}/efetivar`
- `POST /api/financeiro/despesas/{id}/cancelar`
- `POST /api/financeiro/despesas/{id}/estornar`

`PUT` e `POST /cancelar` aceitam query param opcional `escopoRecorrencia`:
- `1` Apenas essa
- `2` Essa e as proximas
- `3` Todas pendentes

Quando informado com valor fora do enum, a API retorna `escopo_recorrencia_invalido` (400).

## Contrato de listagem
### Query params
- `id` (opcional)
- `descricao` (opcional)
- `competencia` (opcional)
- `dataInicio` (opcional, `yyyy-MM-dd`)
- `dataFim` (opcional, `yyyy-MM-dd`)
- `verificarUltimaRecorrencia` (opcional, `bool`, default `false`)

### Regras
- se `dataInicio` e `dataFim` forem informadas, `dataFim >= dataInicio` (`periodo_invalido` em caso contrario)
- se `competencia`, `dataInicio` e `dataFim` nao forem enviados, a API aplica automaticamente o periodo da competencia atual
- despesas espelho de rateio com status `pendenteaprovacao` ou `rejeitado` nao entram na listagem principal
- `verificarUltimaRecorrencia` e repassado para o service como parte do filtro de listagem

### Exemplo de response de sucesso (200)
```json
[
  {
    "id": 12,
    "descricao": "Almoco com cliente",
    "dataLancamento": "2026-03-10",
    "dataVencimento": "2026-03-15",
    "dataEfetivacao": null,
    "tipoDespesa": "alimentacao",
    "tipoPagamento": "pix",
    "valorTotal": 150.0,
    "valorLiquido": 145.0,
    "valorEfetivacao": null,
    "status": "pendente",
    "vinculo": {
      "contaBancariaId": 3,
      "cartaoId": null
    }
  }
]
```

## Contrato de detalhe
`GET /api/financeiro/despesas/{id}` retorna `DespesaDto` com:
- dados principais da despesa
- `recorrencia`, `quantidadeRecorrencia`, `recorrenciaFixa`
- `amigosRateio` e `areasSubAreasRateio`
- `documentos`
- `vinculo` (`contaBancariaId`, `cartaoId`)
- `logs`

## Payload de criacao
### Request (`POST /api/financeiro/despesas`)
```json
{
  "descricao": "Almoco com cliente",
  "observacao": "Reuniao comercial",
  "dataLancamento": "2026-03-10",
  "dataVencimento": "2026-03-15",
  "tipoDespesa": "alimentacao",
  "tipoPagamento": "pix",
  "recorrencia": 1,
  "valorTotal": 150.0,
  "desconto": 5.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "documentos": [
    {
      "nomeArquivo": "recibo.pdf",
      "conteudoBase64": "<base64>",
      "contentType": "application/pdf"
    }
  ],
  "amigosRateio": [
    {
      "amigoId": 10,
      "valor": 150.0
    }
  ],
  "areasSubAreasRateio": [
    {
      "areaId": 1,
      "subAreaId": 3,
      "valor": 150.0
    }
  ],
  "quantidadeRecorrencia": 1,
  "quantidadeParcelas": null,
  "recorrenciaFixa": false,
  "vinculo": {
    "contaBancariaId": 3,
    "cartaoId": null
  }
}
```

### Regras de criacao/atualizacao
- `descricao` obrigatoria (`descricao_obrigatoria`)
- `valorTotal > 0` (`valor_total_invalido`)
- `dataVencimento >= dataLancamento` (`periodo_invalido`)
- `tipoDespesa`, `tipoPagamento` e `recorrencia` devem ser validos (`enum_invalida`)
- status inicial sempre `pendente`
- `valorLiquido` e recalculado no backend: `valorTotal - desconto + acrescimo + imposto + juros`
- atualizacao so e permitida quando status atual for `pendente` (`status_invalido`)
- em recorrencias, o backend aplica `escopoRecorrencia` somente sobre itens `pendente`
- ao editar `essa e as proximas` ou `todas pendentes`, os novos dados sao replicados e as datas sao recalculadas respeitando a recorrencia

### Regras de recorrencia
- `recorrenciaFixa = true` exige recorrencia diferente de `Unica` (`recorrencia_fixa_invalida`)
- quando `recorrenciaFixa = false` e recorrencia nao for `Unica`, `quantidadeRecorrencia` deve ser maior que zero
- quando informada, `quantidadeRecorrencia` nao pode exceder 100 (nao fixa)
- para `cartaoCredito` e `cartaoDebito`:
  - recorrencia e forcada para `Mensal`
  - `recorrenciaFixa` e forcada para `false`
  - e obrigatorio informar parcelas (`quantidadeParcelas` ou `quantidadeRecorrencia`) > 0 (`quantidade_parcelas_invalida`)
- quando o total de recorrencias for maior que 1, a API publica criacao em background para lancamentos futuros

### Regras de vinculo de pagamento
- nao pode informar `contaBancariaId` e `cartaoId` ao mesmo tempo (`forma_pagamento_invalida`)
- `pix` e `transferencia` exigem `contaBancariaId` (`conta_bancaria_obrigatoria`)
- `cartaoCredito` e `cartaoDebito` exigem `cartaoId` (`cartao_obrigatorio`)
- conta/cartao informados precisam existir para o usuario (`conta_bancaria_invalida`, `cartao_invalido`)

### Regras de rateio
- amigos:
  - ids unicos e validos (> 0)
  - cada item com `valor > 0`
  - soma exata dos valores = `valorTotal`
  - amigo precisa ser amizade aceita e usuario ativo (`amigo_rateio_invalido`)
- area/subarea:
  - `areaId` e `subAreaId` > 0
  - subarea deve pertencer a area informada (`relacao_area_subarea_invalida`)
  - area/subarea deve ser do tipo financeiro `Despesa` (`area_subarea_invalida`)
  - soma dos valores = `valorTotal` (`rateio_area_invalido`)

## Efetivacao
### Request (`POST /api/financeiro/despesas/{id}/efetivar`)
```json
{
  "dataEfetivacao": "2026-03-15",
  "tipoPagamento": "pix",
  "valorTotal": 150.0,
  "desconto": 5.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "documentos": [],
  "contaBancariaId": 3,
  "cartaoId": null,
  "vinculo": {
    "contaBancariaId": 3,
    "cartaoId": null
  }
}
```

### Regras
- so efetiva despesa em status `pendente` (`status_invalido`)
- `tipoPagamento` obrigatorio e `valorTotal > 0` (`dados_invalidos`)
- `dataEfetivacao >= dataLancamento` (`periodo_invalido`)
- aplica regras de vinculo (conta/cartao)
- define:
  - `status = efetivada`
  - `dataEfetivacao`
  - `valorLiquido` recalculado
  - `valorEfetivacao = valorLiquido`
- registra historico financeiro de efetivacao

## Cancelamento
`POST /api/financeiro/despesas/{id}/cancelar`

Regras:
- so permite cancelar em status `pendente`
- define `status = cancelada`
- para recorrencia:
  - `1`: cancela apenas a despesa atual
  - `2`: cancela a atual e as proximas pendentes da serie
  - `3`: cancela todas as pendentes da serie
- em recorrencia fixa, cancelar `todas pendentes` encerra a geracao futura da serie

## Estorno
`POST /api/financeiro/despesas/{id}/estornar`

Regras:
- so permite estornar em status `efetivada`
- define:
  - `status = pendente`
  - `dataEfetivacao = null`
  - `valorEfetivacao = null`
- registra historico financeiro de estorno

## Enumeracoes relevantes
### `tipoDespesa`
- `alimentacao`
- `transporte`
- `moradia`
- `lazer`
- `saude`
- `educacao`
- `servicos`

### `tipoPagamento`
- `pix`
- `cartaoCredito`
- `cartaoDebito`
- `boleto`
- `transferencia`
- `dinheiro`

### `status` retornado pela API
- `pendente`
- `efetivada`
- `cancelada`
- `pendenteaprovacao`
- `rejeitado`

### `recorrencia` (enum numerico)
- `1` Unica
- `2` Diaria
- `3` Semanal
- `4` Quinzenal
- `5` Mensal
- `6` Trimestral
- `7` Semestral
- `8` Anual

## Erros e formato de resposta
- erros de dominio e validacao: `400`
- recurso nao encontrado: `404`
- erro interno: `500`

Formato padrao (`application/problem+json`):
```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Requisicao invalida",
  "status": 400,
  "detail": "A operacao nao pode ser realizada no status atual.",
  "instance": "/api/financeiro/despesas/12/efetivar",
  "code": "status_invalido",
  "traceId": "00-..."
}
```

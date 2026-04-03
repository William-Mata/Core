# Tela de Receita

## Objetivo
Documentar o contrato atual da API para a tela de receita, com base no comportamento real do backend.

Arquivos fonte usados:
- `Core.Api/Controllers/Financeiro/ReceitaController.cs`
- `Core.Application/Services/Financeiro/ReceitaService.cs`
- `Core.Application/DTOs/Financeiro/ReceitaDtos.cs`
- `Core.Domain/Enums/Recorrencia.cs`
- `Core.Domain/Enums/StatusReceita.cs`

## Autenticacao
Todos os endpoints exigem autenticacao (`[Authorize]`).

## Endpoints
- `GET /api/financeiro/receitas`
- `GET /api/financeiro/receitas/{id}`
- `POST /api/financeiro/receitas`
- `PUT /api/financeiro/receitas/{id}`
- `POST /api/financeiro/receitas/{id}/efetivar`
- `POST /api/financeiro/receitas/{id}/cancelar`
- `POST /api/financeiro/receitas/{id}/estornar`

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
- se `dataInicio` e `dataFim` vierem juntas, `dataFim >= dataInicio` (`periodo_invalido`)
- sem `competencia`, `dataInicio` e `dataFim`, a API aplica automaticamente a competencia atual
- receitas espelho de rateio com status `pendenteaprovacao` ou `rejeitado` nao entram na listagem principal
- `verificarUltimaRecorrencia` e repassado para o service como parte do filtro de listagem

### Exemplo de response de sucesso (200)
```json
[
  {
    "id": 24,
    "descricao": "Freelance",
    "dataLancamento": "2026-03-12",
    "dataVencimento": "2026-03-20",
    "dataEfetivacao": null,
    "tipoReceita": "freelance",
    "tipoRecebimento": "pix",
    "valorTotal": 1200.0,
    "valorLiquido": 1200.0,
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
`GET /api/financeiro/receitas/{id}` retorna `ReceitaDto` com:
- dados principais da receita
- `recorrencia`, `quantidadeRecorrencia`, `recorrenciaFixa`
- `amigosRateio` e `areasSubAreasRateio`
- `contaBancaria` (legado, string)
- `documentos`
- `vinculo` (`contaBancariaId`, `cartaoId`)
- `logs`

## Payload de criacao
### Request (`POST /api/financeiro/receitas`)
```json
{
  "descricao": "Freelance design",
  "observacao": "Projeto pontual",
  "dataLancamento": "2026-03-12",
  "dataVencimento": "2026-03-20",
  "tipoReceita": "freelance",
  "tipoRecebimento": "pix",
  "recorrencia": 1,
  "valorTotal": 1200.0,
  "desconto": 0.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "areasSubAreasRateio": [
    {
      "areaId": 2,
      "subAreaId": 8,
      "valor": 1200.0
    }
  ],
  "contaBancaria": "Conta Principal",
  "documentos": [
    {
      "nomeArquivo": "comprovante.pdf",
      "conteudoBase64": "<base64>",
      "contentType": "application/pdf"
    }
  ],
  "amigosRateio": [
    {
      "amigoId": 10,
      "valor": 1200.0
    }
  ],
  "quantidadeRecorrencia": 1,
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
- `tipoReceita`, `tipoRecebimento` e `recorrencia` validos (`enum_invalida`)
- status inicial sempre `pendente`
- `valorLiquido` calculado no backend: `valorTotal - desconto + acrescimo + imposto + juros`
- atualizacao somente em status `pendente` (`status_invalido`)
- em recorrencias, o backend aplica `escopoRecorrencia` somente sobre itens `pendente`
- ao editar `essa e as proximas` ou `todas pendentes`, os novos dados sao replicados e as datas sao recalculadas respeitando a recorrencia

### Regras de recorrencia
- `recorrenciaFixa = true` nao permite `recorrencia = Unica` (`recorrencia_fixa_invalida`)
- se recorrencia nao for `Unica` e nao for fixa, `quantidadeRecorrencia` deve ser > 0
- em recorrencia nao fixa, `quantidadeRecorrencia` (quando informada) nao pode ser > 100
- quando alvo de recorrencia for maior que 1, a API publica criacao em background

### Regras de recebimento e vinculo
- tipos que exigem conta bancaria: `pix`, `transferencia`, `contaCorrente`
- para `cartaoCredito` e `cartaoDebito`, `cartaoId` e obrigatorio
- nao pode enviar conta e cartao ao mesmo tempo (`forma_pagamento_invalida`)
- `contaBancaria` (legado) aceita id em texto ou descricao da conta; backend resolve para `contaBancariaId`
- conta/cartao precisam existir para o usuario (`conta_bancaria_invalida`, `cartao_invalido`)

### Regras de rateio
- amigos:
  - ids validos e sem repeticao
  - cada item com `valor > 0`
  - soma dos valores = `valorTotal`
  - amigo deve ser aceito e ativo (`amigo_rateio_invalido`)
- area/subarea:
  - ids > 0
  - subarea deve pertencer a area (`relacao_area_subarea_invalida`)
  - area/subarea precisa ser do tipo financeiro `Receita`
  - soma dos valores = `valorTotal`

## Efetivacao
### Request (`POST /api/financeiro/receitas/{id}/efetivar`)
```json
{
  "dataEfetivacao": "2026-03-20",
  "tipoRecebimento": "pix",
  "contaBancaria": "Conta Principal",
  "valorTotal": 1200.0,
  "desconto": 0.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "documentos": [],
  "vinculo": {
    "contaBancariaId": 3,
    "cartaoId": null
  }
}
```

### Regras
- so efetiva receita em status `pendente`
- `tipoRecebimento` obrigatorio e `valorTotal > 0`
- `dataEfetivacao >= dataLancamento`
- aplica validacoes de conta/cartao
- define:
  - `status = efetivada`
  - `dataEfetivacao`
  - `valorLiquido` recalculado
  - `valorEfetivacao = valorLiquido`
- registra historico financeiro de efetivacao

## Cancelamento
`POST /api/financeiro/receitas/{id}/cancelar`

Regras:
- permitido somente em status `pendente`
- define `status = cancelada`
- para recorrencia:
  - `1`: cancela apenas a receita atual
  - `2`: cancela a atual e as proximas pendentes da serie
  - `3`: cancela todas as pendentes da serie
- em recorrencia fixa, cancelar `todas pendentes` encerra a geracao futura da serie

## Estorno
`POST /api/financeiro/receitas/{id}/estornar`

Regras:
- permitido somente em status `efetivada`
- define:
  - `status = pendente`
  - `dataEfetivacao = null`
  - `valorEfetivacao = null`
- registra historico financeiro de estorno

## Enumeracoes relevantes
### `tipoReceita`
- `salario`
- `freelance`
- `reembolso`
- `investimento`
- `bonus`
- `outros`

### `tipoRecebimento`
- `pix`
- `transferencia`
- `contaCorrente`
- `dinheiro`
- `boleto`
- `cartaoCredito`
- `cartaoDebito`

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
- payload de erro segue `application/problem+json` com `code` e `traceId`

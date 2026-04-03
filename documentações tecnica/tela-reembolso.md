# Tela de Reembolso

## Objetivo
Documentar o contrato atual da API para a tela de reembolso com as regras vigentes no backend.

Arquivos fonte usados:
- `Core.Api/Controllers/Financeiro/ReembolsoController.cs`
- `Core.Application/Services/Financeiro/ReembolsoService.cs`
- `Core.Application/DTOs/Financeiro/ReembolsoDtos.cs`
- `Core.Domain/Entities/Financeiro/Reembolso.cs`
- `Core.Domain/Enums/StatusReembolso.cs`

## Autenticacao
Todos os endpoints exigem autenticacao (`[Authorize]`).

## Endpoints
- `GET /api/financeiro/reembolsos`
- `GET /api/financeiro/reembolsos/{id}`
- `POST /api/financeiro/reembolsos`
- `PUT /api/financeiro/reembolsos/{id}`
- `DELETE /api/financeiro/reembolsos/{id}`
- `POST /api/financeiro/reembolsos/{id}/efetivar`
- `POST /api/financeiro/reembolsos/{id}/estornar`

## Contrato de listagem
### Query params
- `id` (opcional)
- `descricao` (opcional)
- `competencia` (opcional)
- `dataInicio` (opcional, `yyyy-MM-dd`)
- `dataFim` (opcional, `yyyy-MM-dd`)

### Regras
- `dataFim` nao pode ser menor que `dataInicio` (`periodo_invalido`)
- sem `competencia`, `dataInicio` e `dataFim`, a API aplica automaticamente competencia atual

### Response (200)
```json
[
  {
    "id": 1,
    "descricao": "Viagem comercial",
    "solicitante": "Joao Silva",
    "dataLancamento": "2026-03-18",
    "dataEfetivacao": null,
    "valorTotal": 274.9,
    "status": "AGUARDANDO"
  }
]
```

## Contrato de detalhe
`GET /api/financeiro/reembolsos/{id}` retorna:
- `id`
- `descricao`
- `solicitante`
- `dataLancamento`
- `dataEfetivacao`
- `despesasVinculadas` (array de ids)
- `documentos`
- `valorTotal`
- `status` (sempre em UPPERCASE)

## Payload de criacao/atualizacao
### Request (`POST` e `PUT`)
```json
{
  "descricao": "Viagem comercial - semana 2",
  "solicitante": "Joao Silva",
  "dataLancamento": "2026-03-18",
  "dataEfetivacao": null,
  "despesasVinculadas": [1, { "id": 3 }],
  "valorTotal": 9999.99,
  "status": "AGUARDANDO",
  "documentos": [
    {
      "nomeArquivo": "nota.pdf",
      "conteudoBase64": "<base64>",
      "contentType": "application/pdf"
    }
  ],
  "contaBancariaId": null,
  "cartaoId": null
}
```

### Regras de validacao
- `descricao` obrigatoria (`descricao_obrigatoria`)
- `solicitante` obrigatorio (`solicitante_obrigatorio`)
- `despesasVinculadas` obrigatoria, com ao menos um id valido (`despesas_vinculadas_obrigatorias`)
- cada item em `despesasVinculadas` aceita:
  - numero (`1`)
  - objeto com `{ "id": 1 }`
- formato invalido de item gera `despesa_vinculada_invalida`
- ids sao normalizados para valores > 0 e unicos
- todas as despesas precisam existir para o usuario (`despesa_nao_encontrada`)
- uma despesa nao pode estar vinculada a outro reembolso (`despesa_vinculada_outro_reembolso`)
  - no update, o proprio reembolso e ignorado nessa checagem
- nao pode informar `contaBancariaId` e `cartaoId` juntos (`forma_pagamento_invalida`)

### Regras de status
Status aceitos na request (`case-insensitive`):
- `AGUARDANDO`
- `APROVADO`
- `PAGO`
- `CANCELADO`
- `REJEITADO`

Comportamento:
- se `status` nao for enviado, default = `AGUARDANDO`
- status invalido gera `status_reembolso_invalido`
- response sempre retorna status em UPPERCASE

### Regras de data de efetivacao
- se `status = PAGO`, `dataEfetivacao` e obrigatoria (`data_efetivacao_obrigatoria`)
- se informada, `dataEfetivacao` deve ser `>= dataLancamento` (`periodo_invalido`)

### Regras de valor
- `valorTotal` recebido na request nao e fonte de verdade
- backend sempre recalcula: `valorTotal = soma(ValorTotal das despesas vinculadas)`

### Efeitos colaterais no update
- transicao de `status != PAGO` para `PAGO`:
  - exige destino de pagamento (`contaBancariaId` ou `cartaoId`)
  - registra historico financeiro de efetivacao
- transicao de `PAGO` para qualquer outro status:
  - registra historico financeiro de estorno

## Efetivar reembolso
### Endpoint
`POST /api/financeiro/reembolsos/{id}/efetivar`

### Request
```json
{
  "dataEfetivacao": "2026-03-20",
  "documentos": [],
  "contaBancariaId": 3,
  "cartaoId": null
}
```

### Regras
- nao permite efetivar se ja estiver `PAGO` (`status_invalido`)
- `dataEfetivacao >= dataLancamento`
- exige exatamente um destino de pagamento:
  - conta OU cartao
  - sem ambos ao mesmo tempo
- define:
  - `status = PAGO`
  - `dataEfetivacao`
- registra historico financeiro de efetivacao

## Estornar reembolso
### Endpoint
`POST /api/financeiro/reembolsos/{id}/estornar`

### Regras
- somente para reembolso em `PAGO`
- define:
  - `status = AGUARDANDO`
  - `dataEfetivacao = null`
- registra historico financeiro de estorno

## Excluir reembolso
### Endpoint
`DELETE /api/financeiro/reembolsos/{id}`

### Regras
- remove o recurso quando encontrado
- response: `204 No Content`

## Estrutura da entidade (backend)
Campos principais de `Reembolso`:
- `Id`
- `DataHoraCadastro` (UTC)
- `UsuarioCadastroId`
- `Descricao`
- `Solicitante`
- `DataLancamento`
- `DataEfetivacao`
- `Documentos`
- `ValorTotal`
- `Status`
- `Despesas`

## Erros e formato de resposta
- erros de dominio e validacao: `400`
- nao encontrado: `404`
- erro interno: `500`

Formato padrao de erro: `application/problem+json` com `code` e `traceId`.

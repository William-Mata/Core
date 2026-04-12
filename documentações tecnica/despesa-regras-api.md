# DespesaController - Regras de API

## Objetivo
Documentar o contrato real do `DespesaController`, com regras de negocio do service, exemplos JSON e erros esperados.

## Autenticacao
- Todas as rotas exigem JWT Bearer.
- Header: `Authorization: Bearer <token>`.

## Rotas
- `GET /api/financeiro/despesas`
- `GET /api/financeiro/despesas/{id}`
- `POST /api/financeiro/despesas`
- `PUT /api/financeiro/despesas/{id}`
- `POST /api/financeiro/despesas/{id}/efetivar`
- `POST /api/financeiro/despesas/{id}/cancelar`
- `POST /api/financeiro/despesas/{id}/estornar`
- `GET /api/financeiro/despesas/pendentes-aprovacao`
- `POST /api/financeiro/despesas/{id}/aprovar`
- `POST /api/financeiro/despesas/{id}/rejeitar`

## Enums usados no contrato
- `TipoDespesa`: `Alimentacao`, `Transporte`, `Moradia`, `Lazer`, `Saude`, `Educacao`, `Servicos`
- `TipoPagamento`: `Pix`, `CartaoCredito`, `CartaoDebito`, `Boleto`, `Transferencia`, `Dinheiro`
- `Recorrencia`: `Unica`, `Diaria`, `Semanal`, `Quinzenal`, `Mensal`, `Trimestral`, `Semestral`, `Anual`
- `EscopoRecorrencia`: `ApenasEssa`, `EssaEAsProximas`, `TodasPendentes`

## Regras globais
- Exige usuario autenticado (`usuario_nao_autenticado`).
- `valorLiquido = valorTotal - desconto + acrescimo + imposto + juros`.
- Criacao inicia com `status = Pendente`.
- Atualizacao/cancelamento/efetivacao/estorno validam status atual da despesa.
- `Pix` e `Transferencia` exigem `contaBancariaId`.
- `CartaoCredito` e `CartaoDebito` exigem `cartaoId` e nao permitem `contaBancariaId`.
- Efetivacao e estorno registram historico financeiro.
- Em `Transferencia` ou `Pix` com `contaDestinoId`, historico gera movimentacao espelhada automaticamente.
- Em transacao entre contas (`Transferencia` ou `Pix` com `contaDestinoId`), a API cria e vincula uma `Receita` espelhada.
- A movimentacao espelhada permanece sincronizada com a origem em edicao, cancelamento e efetivacao.

## 1) Listar despesas
### GET `/api/financeiro/despesas`
#### Query params
- `id` (opcional)
- `descricao` (opcional)
- `competencia` (opcional)
- `dataInicio` (opcional)
- `dataFim` (opcional)
- `verificarUltimaRecorrencia` (opcional)

#### Regras
- Se `dataInicio > dataFim`, retorna `periodo_invalido`.
- Se nenhum periodo for informado, aplica competencia atual.

#### Exemplo de resposta (200)
```json
[
  {
    "id": 10,
    "descricao": "Aluguel",
    "dataLancamento": "2026-04-01",
    "dataVencimento": "2026-04-10",
    "dataEfetivacao": null,
    "tipoDespesa": "Moradia",
    "tipoPagamento": "Pix",
    "valorTotal": 1200.00,
    "valorLiquido": 1200.00,
    "valorEfetivacao": null,
    "status": "pendente",
    "contaBancariaId": 1,
    "contaDestinoId": null,
    "cartaoId": null
  }
]
```

## 2) Obter despesa por id
### GET `/api/financeiro/despesas/{id}`
#### Erros comuns
- `despesa_nao_encontrada`

#### Exemplo de resposta (200)
```json
{
  "id": 10,
  "descricao": "Aluguel",
  "observacao": null,
  "dataLancamento": "2026-04-01",
  "dataVencimento": "2026-04-10",
  "dataEfetivacao": null,
  "tipoDespesa": "Moradia",
  "tipoPagamento": "Pix",
  "recorrencia": "Mensal",
  "quantidadeRecorrencia": 12,
  "recorrenciaFixa": false,
  "valorTotal": 1200.00,
  "valorTotalRateioAmigos": null,
  "valorLiquido": 1200.00,
  "desconto": 0,
  "acrescimo": 0,
  "imposto": 0,
  "juros": 0,
  "valorEfetivacao": null,
  "status": "pendente",
  "tipoRateioAmigos": "Comum",
  "amigosRateio": [
    {
      "amigoId": 2,
      "nome": "Alex",
      "valor": 100.00
    }
  ],
  "areasSubAreasRateio": [
    {
      "areaId": 1,
      "areaNome": "Casa",
      "subAreaId": 10,
      "subAreaNome": "Aluguel",
      "valor": 1200.00
    }
  ],
  "contaBancariaId": 1,
  "contaDestinoId": null,
  "cartaoId": null,
  "documentos": [
    {
      "nomeArquivo": "nota.pdf",
      "caminhoArquivo": "/docs/nota.pdf",
      "contentType": "application/pdf",
      "tamanhoBytes": 102400
    }
  ],
  "logs": [
    {
      "id": 1,
      "data": "2026-04-01",
      "acao": "Cadastro",
      "descricao": "Despesa criada com status pendente."
    }
  ]
}
```

## 3) Criar despesa
### POST `/api/financeiro/despesas`
#### Exemplo de request (payload completo)
```json
{
  "descricao": "Mercado",
  "observacao": "Compra mensal",
  "dataLancamento": "2026-04-11",
  "dataVencimento": "2026-04-11",
  "tipoDespesa": "Alimentacao",
  "tipoPagamento": "Pix",
  "recorrencia": "Unica",
  "valorTotal": 350.00,
  "desconto": 0,
  "acrescimo": 0,
  "imposto": 0,
  "juros": 0,
  "documentos": [
    {
      "nomeArquivo": "nota-mercado.pdf",
      "contentType": "application/pdf",
      "base64": "JVBERi0xLjQKJ..."
    }
  ],
  "amigosRateio": [
    {
      "amigoId": 2,
      "valor": 100.00
    }
  ],
  "areasSubAreasRateio": [
    {
      "areaId": 1,
      "subAreaId": 10,
      "valor": 350.00
    }
  ],
  "quantidadeRecorrencia": 1,
  "quantidadeParcelas": null,
  "recorrenciaFixa": false,
  "valorLiquido": null,
  "contaBancariaId": 10,
  "contaDestinoId": null,
  "cartaoId": null,
  "valorTotalRateioAmigos": 500.00,
  "tipoRateioAmigos": "Comum"
}
```

#### Regras principais
- `descricao` obrigatoria.
- `valorTotal > 0`.
- `dataVencimento >= dataLancamento`.
- Enums validos; caso contrario `enum_invalida`.
- Regra de meio financeiro (`conta_bancaria_obrigatoria`, `cartao_obrigatorio`, `forma_pagamento_invalida`).
- Regras de recorrencia/parcelamento.
- `contaDestinoId` pode ser enviada no cadastro/edicao.
- Se `tipoPagamento = Transferencia` ou `Pix` e `contaDestinoId` for informada:
  - nao pode ser igual a `contaBancariaId` (`conta_destino_invalida`).
  - a conta destino precisa pertencer ao usuario (`conta_bancaria_invalida`).

#### Exemplo de resposta (201)
- Retorna `DespesaDto` completo.

## 4) Atualizar despesa
### PUT `/api/financeiro/despesas/{id}`
#### Query param
- `escopoRecorrencia` (opcional, default `ApenasEssa`)

#### Regras principais
- Despesa precisa estar `Pendente`.
- Respeita mesmas validacoes de criacao.
- Atualiza somente alvos permitidos pelo escopo.

#### Exemplo de request (payload completo)
```json
{
  "descricao": "Mercado atualizado",
  "observacao": "Ajuste de itens",
  "dataLancamento": "2026-04-11",
  "dataVencimento": "2026-04-11",
  "tipoDespesa": "Alimentacao",
  "tipoPagamento": "Pix",
  "recorrencia": "Unica",
  "valorTotal": 360.00,
  "desconto": 0,
  "acrescimo": 0,
  "imposto": 0,
  "juros": 0,
  "documentos": [
    {
      "nomeArquivo": "nota-atualizada.pdf",
      "contentType": "application/pdf",
      "base64": "JVBERi0xLjQKJ..."
    }
  ],
  "amigosRateio": [
    {
      "amigoId": 2,
      "valor": 120.00
    }
  ],
  "areasSubAreasRateio": [
    {
      "areaId": 1,
      "subAreaId": 10,
      "valor": 360.00
    }
  ],
  "quantidadeRecorrencia": 1,
  "quantidadeParcelas": null,
  "recorrenciaFixa": false,
  "valorLiquido": null,
  "contaBancariaId": 10,
  "contaDestinoId": null,
  "cartaoId": null,
  "valorTotalRateioAmigos": 520.00,
  "tipoRateioAmigos": "Comum"
}
```

## 5) Efetivar despesa
### POST `/api/financeiro/despesas/{id}/efetivar`
#### Exemplo de request (payload completo - transferencia entre contas)
```json
{
  "dataEfetivacao": "2026-04-11",
  "tipoPagamento": "Transferencia",
  "valorTotal": 150.00,
  "desconto": 0,
  "acrescimo": 0,
  "imposto": 0,
  "juros": 0,
  "documentos": [
    {
      "nomeArquivo": "comprovante-transferencia.pdf",
      "contentType": "application/pdf",
      "base64": "JVBERi0xLjQKJ..."
    }
  ],
  "contaBancariaId": 10,
  "cartaoId": null,
  "contaDestinoId": 20,
  "observacaoHistorico": "Transferencia entre contas"
}
```

#### Regras
- Exige status `Pendente`.
- `dataEfetivacao >= dataLancamento`.
- `contaDestinoId` e opcional.
- Se informado em `Transferencia` ou `Pix`:
  - nao pode ser igual a conta de origem (`conta_destino_invalida`).
  - conta destino deve pertencer ao usuario (`conta_bancaria_invalida`).
- Mantem comportamento anterior para tipos diferentes de `Transferencia` e `Pix`.

#### Efeito colateral
- Registra historico de efetivacao.
- Em `Transferencia` ou `Pix` com `contaDestinoId`, gera espelho no historico:
  - origem `Despesa` e destino `Receita`.

## 6) Cancelar despesa
### POST `/api/financeiro/despesas/{id}/cancelar`
#### Query param
- `escopoRecorrencia` (opcional, default `ApenasEssa`)

#### Regras
- Exige status `Pendente`.
- Cancela a despesa ou serie conforme escopo.

## 7) Estornar despesa
### POST `/api/financeiro/despesas/{id}/estornar`
#### Exemplo de request (payload completo)
```json
{
  "dataEstorno": "2026-04-12",
  "observacaoHistorico": "Estorno transferencia",
  "ocultarDoHistorico": true,
  "contaDestinoId": 20
}
```

#### Regras
- Exige status `Efetivada`.
- `dataEstorno` obrigatoria.
- `dataEstorno >= dataLancamento`.
- Se houver `dataEfetivacao`, `dataEstorno >= dataEfetivacao`.
- Em `Transferencia` ou `Pix`, se `contaDestinoId` nao for enviada, tenta reaproveitar do ultimo historico.

#### Efeito colateral
- Registra historico de estorno.
- Em `Transferencia` ou `Pix` com conta destino valida, gera espelho de estorno no historico.

## 8) Pendentes de aprovacao
### GET `/api/financeiro/despesas/pendentes-aprovacao`
- Retorna despesas espelho pendentes para aprovacao do usuario.

## 9) Aprovar rateio
### POST `/api/financeiro/despesas/{id}/aprovar`
- Exige despesa espelho (`DespesaOrigemId` preenchido).
- Exige status `PendenteAprovacao`.

## 10) Rejeitar rateio
### POST `/api/financeiro/despesas/{id}/rejeitar`
- Exige despesa espelho (`DespesaOrigemId` preenchido).
- Exige status `PendenteAprovacao`.

## Erros de negocio comuns
- `despesa_nao_encontrada`
- `dados_invalidos`
- `periodo_invalido`
- `status_invalido`
- `conta_bancaria_obrigatoria`
- `conta_bancaria_invalida`
- `cartao_obrigatorio`
- `cartao_invalido`
- `forma_pagamento_invalida`
- `conta_destino_invalida`

## Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/DespesaController.cs`
- Service: `Core.Application/Services/Financeiro/DespesaService.cs`
- DTOs: `Core.Application/DTOs/Financeiro/DespesaDtos.cs`

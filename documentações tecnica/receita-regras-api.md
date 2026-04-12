# ReceitaController - Regras de API

## Objetivo
Documentar o contrato real do `ReceitaController`, com regras de negocio do service, exemplos JSON e erros esperados.

## Autenticacao
- Todas as rotas exigem JWT Bearer.
- Header: `Authorization: Bearer <token>`.

## Rotas
- `GET /api/financeiro/receitas`
- `GET /api/financeiro/receitas/{id}`
- `POST /api/financeiro/receitas`
- `PUT /api/financeiro/receitas/{id}`
- `POST /api/financeiro/receitas/{id}/efetivar`
- `POST /api/financeiro/receitas/{id}/cancelar`
- `POST /api/financeiro/receitas/{id}/estornar`
- `GET /api/financeiro/receitas/pendentes-aprovacao`
- `POST /api/financeiro/receitas/{id}/aprovar`
- `POST /api/financeiro/receitas/{id}/rejeitar`

## Enums usados no contrato
- `TipoReceita`: `Salario`, `Freelance`, `Reembolso`, `Investimento`, `Bonus`, `Outros`
- `TipoRecebimento`: `Pix`, `Transferencia`, `Dinheiro`, `Boleto`, `CartaoCredito`, `CartaoDebito`
- `Recorrencia`: `Unica`, `Diaria`, `Semanal`, `Quinzenal`, `Mensal`, `Trimestral`, `Semestral`, `Anual`
- `EscopoRecorrencia`: `ApenasEssa`, `EssaEAsProximas`, `TodasPendentes`

## Regras globais
- Exige usuario autenticado (`usuario_nao_autenticado`).
- `valorLiquido = valorTotal - desconto + acrescimo + imposto + juros`.
- Criacao inicia com `status = Pendente`.
- `competencia` e a fonte de verdade para cadastro, edicao e listagem.
- `competencia` armazena apenas mes/ano no formato `yyyy-MM`.
- Quando `competencia` nao for informada, a API assume a competencia atual.
- Atualizacao/cancelamento/efetivacao/estorno validam status atual da receita.
- `Pix` e `Transferencia` exigem `contaBancariaId`.
- `CartaoCredito` e `CartaoDebito` exigem `cartaoId` e nao permitem `contaBancariaId`.
- Efetivacao e estorno registram historico financeiro.
- Em `Transferencia` ou `Pix` com `contaDestinoId`, historico gera movimentacao espelhada automaticamente.
- Em transacao entre contas (`Transferencia` ou `Pix` com `contaDestinoId`), a API cria e vincula uma `Despesa` espelhada.
- A movimentacao espelhada permanece sincronizada com a origem em edicao, cancelamento e efetivacao.

## 1) Listar receitas
### GET `/api/financeiro/receitas`
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
- Quando `competencia` for informada, a listagem filtra pela competencia normalizada e nao depende de `dataLancamento`.

#### Exemplo de resposta (200)
```json
[
  {
    "id": 21,
    "descricao": "Salario",
    "dataLancamento": "2026-04-05",
    "dataVencimento": "2026-04-05",
    "dataEfetivacao": null,
    "tipoReceita": "Salario",
    "tipoRecebimento": "Pix",
    "valorTotal": 5000.00,
    "valorLiquido": 5000.00,
    "valorEfetivacao": null,
    "status": "pendente",
    "contaBancariaId": 1,
    "contaDestinoId": null,
    "cartaoId": null
  }
]
```

## 2) Obter receita por id
### GET `/api/financeiro/receitas/{id}`
#### Erros comuns
- `receita_nao_encontrada`

#### Exemplo de resposta (200)
```json
{
  "id": 21,
  "descricao": "Salario",
  "observacao": null,
  "dataLancamento": "2026-04-05",
  "dataVencimento": "2026-04-05",
  "dataEfetivacao": null,
  "tipoReceita": "Salario",
  "tipoRecebimento": "Pix",
  "recorrencia": "Mensal",
  "quantidadeRecorrencia": 12,
  "recorrenciaFixa": false,
  "valorTotal": 5000.00,
  "valorTotalRateioAmigos": null,
  "valorLiquido": 5000.00,
  "desconto": 0,
  "acrescimo": 0,
  "imposto": 0,
  "juros": 0,
  "valorEfetivacao": null,
  "status": "pendente",
  "amigosRateio": [
    {
      "amigoId": 2,
      "nome": "Alex",
      "valor": 300.00
    }
  ],
  "areasSubAreasRateio": [
    {
      "areaId": 2,
      "areaNome": "Trabalho",
      "subAreaId": 21,
      "subAreaNome": "Freelance",
      "valor": 5000.00
    }
  ],
  "contaBancariaId": 1,
  "contaDestinoId": null,
  "cartaoId": null,
  "documentos": [
    {
      "nomeArquivo": "comprovante.pdf",
      "caminhoArquivo": "/docs/comprovante.pdf",
      "contentType": "application/pdf",
      "tamanhoBytes": 204800
    }
  ],
  "logs": [
    {
      "id": 1,
      "data": "2026-04-05",
      "acao": "Cadastro",
      "descricao": "Receita criada com status pendente."
    }
  ]
}
```

## 3) Criar receita
### POST `/api/financeiro/receitas`
#### Exemplo de request (payload completo)
```json
{
  "descricao": "Freelance",
  "observacao": "Projeto abril",
  "dataLancamento": "2026-04-11",
  "dataVencimento": "2026-04-11",
  "tipoReceita": "Freelance",
  "tipoRecebimento": "Pix",
  "recorrencia": "Unica",
  "valorTotal": 900.00,
  "desconto": 0,
  "acrescimo": 0,
  "imposto": 0,
  "juros": 0,
  "areasSubAreasRateio": [
    {
      "areaId": 2,
      "subAreaId": 21,
      "valor": 900.00
    }
  ],
  "documentos": [
    {
      "nomeArquivo": "contrato.pdf",
      "contentType": "application/pdf",
      "base64": "JVBERi0xLjQKJ..."
    }
  ],
  "amigosRateio": [
    {
      "amigoId": 2,
      "valor": 300.00
    }
  ],
  "quantidadeRecorrencia": 1,
  "recorrenciaFixa": false,
  "valorLiquido": null,
  "contaBancariaId": 20,
  "contaDestinoId": null,
  "cartaoId": null,
  "valorTotalRateioAmigos": 1200.00,
  "tipoRateioAmigos": "Comum"
}
```

#### Regras principais
- `descricao` obrigatoria.
- `valorTotal > 0`.
- `dataVencimento >= dataLancamento`.
- `competencia` opcional; quando ausente, assume a competencia atual.
- Enums validos; caso contrario `enum_invalida`.
- Regra de meio financeiro (`conta_bancaria_obrigatoria`, `cartao_obrigatorio`, `forma_pagamento_invalida`).
- Regras de recorrencia.
- `contaDestinoId` pode ser enviada no cadastro/edicao.
- Se `tipoRecebimento = Transferencia` ou `Pix` e `contaDestinoId` for informada:
  - nao pode ser igual a `contaBancariaId` (`conta_destino_invalida`).
  - a conta destino precisa pertencer ao usuario (`conta_bancaria_invalida`).
- Se `contaDestinoId` for enviada com `tipoRecebimento` diferente de `Transferencia`/`Pix`, retorna `conta_destino_invalida`.

#### Exemplo de resposta (201)
- Retorna `ReceitaDto` completo.

## 4) Atualizar receita
### PUT `/api/financeiro/receitas/{id}`
#### Query param
- `escopoRecorrencia` (opcional, default `ApenasEssa`)

#### Regras principais
- Receita precisa estar `Pendente`.
- Respeita mesmas validacoes de criacao.
- Atualiza somente alvos permitidos pelo escopo.
- A competencia salva na receita passa a ser o criterio de consulta da listagem.

#### Exemplo de request (payload completo)
```json
{
  "descricao": "Freelance atualizado",
  "observacao": "Ajuste de valor",
  "dataLancamento": "2026-04-11",
  "dataVencimento": "2026-04-11",
  "tipoReceita": "Freelance",
  "tipoRecebimento": "Pix",
  "recorrencia": "Unica",
  "valorTotal": 950.00,
  "desconto": 0,
  "acrescimo": 0,
  "imposto": 0,
  "juros": 0,
  "areasSubAreasRateio": [
    {
      "areaId": 2,
      "subAreaId": 21,
      "valor": 950.00
    }
  ],
  "documentos": [
    {
      "nomeArquivo": "contrato-atualizado.pdf",
      "contentType": "application/pdf",
      "base64": "JVBERi0xLjQKJ..."
    }
  ],
  "amigosRateio": [
    {
      "amigoId": 2,
      "valor": 320.00
    }
  ],
  "quantidadeRecorrencia": 1,
  "recorrenciaFixa": false,
  "valorLiquido": null,
  "contaBancariaId": 20,
  "contaDestinoId": null,
  "cartaoId": null,
  "valorTotalRateioAmigos": 1250.00,
  "tipoRateioAmigos": "Comum"
}
```

## 5) Efetivar receita
### POST `/api/financeiro/receitas/{id}/efetivar`
#### Exemplo de request (payload completo - transferencia entre contas)
```json
{
  "dataEfetivacao": "2026-04-11",
  "tipoRecebimento": "Transferencia",
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
  "contaBancariaId": 20,
  "cartaoId": null,
  "contaDestinoId": 10,
  "observacaoHistorico": "Transferencia interna"
}
```

#### Regras
- Exige status `Pendente`.
- `dataEfetivacao >= dataLancamento`.
- `contaDestinoId` e opcional.
- Se informado em `Transferencia` ou `Pix`:
  - nao pode ser igual a conta de origem (`conta_destino_invalida`).
  - conta destino deve pertencer ao usuario (`conta_bancaria_invalida`).
- Se `contaDestinoId` for enviada com tipo diferente de `Transferencia`/`Pix`, retorna `conta_destino_invalida`.
- Mantem comportamento anterior para tipos diferentes de `Transferencia` e `Pix`.

#### Efeito colateral
- Registra historico de efetivacao.
- Em `Transferencia` ou `Pix` com `contaDestinoId`, gera espelho no historico:
  - origem `Receita` e destino `Despesa`.

## 6) Cancelar receita
### POST `/api/financeiro/receitas/{id}/cancelar`
#### Query param
- `escopoRecorrencia` (opcional, default `ApenasEssa`)

#### Regras
- Exige status `Pendente`.
- Cancela a receita ou serie conforme escopo.

## 7) Estornar receita
### POST `/api/financeiro/receitas/{id}/estornar`
#### Exemplo de request (payload completo)
```json
{
  "dataEstorno": "2026-04-12",
  "observacaoHistorico": "Estorno transferencia",
  "ocultarDoHistorico": true,
  "contaDestinoId": 10
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
### GET `/api/financeiro/receitas/pendentes-aprovacao`
- Retorna receitas espelho pendentes para aprovacao do usuario.

## 9) Aprovar rateio
### POST `/api/financeiro/receitas/{id}/aprovar`
- Exige receita espelho (`ReceitaOrigemId` preenchido).
- Exige status `PendenteAprovacao`.

## 10) Rejeitar rateio
### POST `/api/financeiro/receitas/{id}/rejeitar`
- Exige receita espelho (`ReceitaOrigemId` preenchido).
- Exige status `PendenteAprovacao`.

## Erros de negocio comuns
- `receita_nao_encontrada`
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
- Controller: `Core.Api/Controllers/Financeiro/ReceitaController.cs`
- Service: `Core.Application/Services/Financeiro/ReceitaService.cs`
- DTOs: `Core.Application/DTOs/Financeiro/ReceitaDtos.cs`

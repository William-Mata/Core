# HistoricoTransacaoFinanceiraController - Regras de API

## Objetivo
Documentar o contrato de consulta do historico financeiro do usuario autenticado.

## Autenticacao
- Todas as rotas exigem JWT Bearer.

## Rotas
- `GET /api/financeiro/historico-transacoes`
- `GET /api/financeiro/historico-transacoes/resumo`
- `GET /api/financeiro/historico-transacoes/resumo-por-ano`

## Regras globais
- Dados sempre filtrados pelo usuario autenticado.
- Registros com `OcultarDoHistorico = true` nao aparecem nas consultas.
- Em `Transferencia` ou `Pix` entre contas com `contaDestinoId`, o historico persiste origem e destino.
- Em `Transferencia` ou `Pix`, pode haver registro espelhado automatico (origem/despesa <-> destino/receita e vice-versa).

## 1) Listar historico
### GET `/api/financeiro/historico-transacoes`
#### Query params
- `quantidadeRegistros` (opcional, default 50, deve ser > 0)
- `ordemRegistros` (opcional, enum: `MaisRecentes` ou `MaisAntigos`)

#### Validacoes
- `quantidadeRegistros <= 0` -> erro de validacao.
- `ordemRegistros` invalido -> erro de validacao.

#### Exemplo de resposta (200)
```json
[
  {
    "id": 101,
    "tipoTransacao": "despesa",
    "tipoOperacao": "efetivacao",
    "descricao": "Transferencia entre contas",
    "dataEfetivacao": "2026-04-11",
    "tipoPagamento": "Transferencia",
    "tipoRecebimento": null,
    "valor": -150.00,
    "tipoDespesa": "Servicos",
    "tipoReceita": null
  },
  {
    "id": 102,
    "tipoTransacao": "receita",
    "tipoOperacao": "efetivacao",
    "descricao": "Transferencia entre contas",
    "dataEfetivacao": "2026-04-11",
    "tipoPagamento": null,
    "tipoRecebimento": "Transferencia",
    "valor": 150.00,
    "tipoDespesa": null,
    "tipoReceita": "Outros"
  }
]
```

## 2) Resumo do historico
### GET `/api/financeiro/historico-transacoes/resumo`
#### Query params
- `ano` (opcional, se informado deve ser > 0)

#### Exemplo de resposta (200)
```json
{
  "ano": 2026,
  "totalEntradas": 10500.00,
  "totalSaidas": 7320.00,
  "totalEfetivacoes": 9980.00,
  "totalEstornos": 520.00
}
```

## 3) Resumo por ano
### GET `/api/financeiro/historico-transacoes/resumo-por-ano`
#### Query params
- `ano` (obrigatorio, > 0)

#### Exemplo de resposta (200)
```json
[
  {
    "mes": 1,
    "totalEntradas": 3000.00,
    "totalSaidas": 2200.00,
    "totalEfetivacoes": 2800.00,
    "totalEstornos": 400.00
  },
  {
    "mes": 2,
    "totalEntradas": 3500.00,
    "totalSaidas": 2400.00,
    "totalEfetivacoes": 3300.00,
    "totalEstornos": 200.00
  }
]
```

## Integracao com efetivacao/estorno de despesa e receita
- `observacaoHistorico` (quando enviada no fluxo de origem) pode ser persistida no historico.
- Em estorno com `ocultarDoHistorico = true`, os registros da transacao sao marcados como ocultos.
- Em `Transferencia` ou `Pix` com `contaDestinoId`:
  - registro de origem guarda `ContaBancariaId` (origem) e `ContaDestinoId` (destino).
  - registro espelhado inverte origem/destino para refletir a outra ponta da movimentacao.

## Erros comuns
- `dados_invalidos` (query invalida)
- `usuario_nao_autenticado`

## Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/HistoricoTransacaoFinanceiraController.cs`
- Service: `Core.Application/Services/Financeiro/HistoricoTransacaoFinanceiraConsultaService.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/HistoricoTransacaoFinanceiraRepository.cs`

# Tela de Histórico de Transações

## Objetivo
Documentar o contrato atual da API para consulta de transações financeiras do histórico.

Arquivos fonte usados:
- `Core.Api/Controllers/Financeiro/HistoricoTransacaoFinanceiraController.cs`
- `Core.Application/Services/Financeiro/HistoricoTransacaoFinanceiraConsultaService.cs`
- `Core.Application/DTOs/Financeiro/HistoricoTransacaoFinanceiraDtos.cs`
- `Core.Domain/Interfaces/Financeiro/IHistoricoTransacaoFinanceiraRepository.cs`
- `Core.Infrastructure/Persistence/Repositories/Financeiro/HistoricoTransacaoFinanceiraRepository.cs`

## Autenticacao
O endpoint exige autenticacao (`[Authorize]`).

## Endpoint
- `GET /api/financeiro/historico-transacoes`

## Contrato de listagem
### Query params
- `quantidadeRegistros` (opcional, `int`, default `50`)
- `ordemRegistros` (opcional, enum, default `MaisRecentes`)

`ordemRegistros`:
- `1` ou `MaisRecentes`
- `2` ou `MaisAntigos`

### Regras
- retorna apenas registros do usuario autenticado (`UsuarioOperacaoId`)
- `quantidadeRegistros` deve ser maior que zero (`quantidade_registros_invalida`)
- `ordemRegistros` deve ser valor valido do enum (`ordem_registros_invalida`)
- ordenacao:
  - `MaisRecentes`: `DataTransacao DESC`, `Id DESC`
  - `MaisAntigos`: `DataTransacao ASC`, `Id ASC`
- aplica `Take(quantidadeRegistros)`

## Campos de retorno
- `idOrigem`: origem da transacao (`despesa`, `receita`, `reembolso`)
- `tipoTransacao`: tipo exibido no historico (`despesa`, `receita`, `reembolso` ou `estorno`)
- `valor`
- `descricao`
- `dataEfetivacao`
- `tipoPagamento` (quando houver)
- `contaBancaria` (quando houver)
- `cartao` (quando houver)
- `tipoDespesa` (quando a origem for despesa)
- `tipoReceita` (quando a origem for receita)

### Exemplo de response de sucesso (200)
```json
[
  {
    "idOrigem": "despesa",
    "tipoTransacao": "estorno",
    "valor": 150.0,
    "descricao": "Estorno de despesa",
    "dataEfetivacao": "2026-03-21",
    "tipoPagamento": "Pix",
    "contaBancaria": "Conta principal",
    "cartao": null,
    "tipoDespesa": "Saude",
    "tipoReceita": null
  },
  {
    "idOrigem": "receita",
    "tipoTransacao": "receita",
    "valor": 1200.0,
    "descricao": "Efetivacao de receita",
    "dataEfetivacao": "2026-03-20",
    "tipoPagamento": "Transferencia",
    "contaBancaria": "Conta salario",
    "cartao": null,
    "tipoDespesa": null,
    "tipoReceita": "Salario"
  }
]
```

## Erros e formato de resposta
- erros de dominio e validacao: `400`
- nao autenticado: `401`
- erro interno: `500`

Formato padrao de erro: `application/problem+json` com `code` e `traceId`.

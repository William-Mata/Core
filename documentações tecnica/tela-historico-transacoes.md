# Tela de Historico de Transacoes

## Objetivo
Documentar o contrato atual da API para consulta de transacoes financeiras no historico.

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
- `GET /api/financeiro/historico-transacoes/resumo`

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
- `idTransacao`: id da transacao de origem (`Despesa.Id`, `Receita.Id` ou `Reembolso.Id`)
- `tipoTransacao`: `despesa`, `receita`, `reembolso`, `estorno despesa`, `estorno receita`, `estorno reembolso`
- `valor`
- `descricao`: descricao da origem (`Despesa.Descricao`, `Receita.Descricao` ou `Reembolso.Descricao`)
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
    "idTransacao": 55,
    "tipoTransacao": "estorno despesa",
    "valor": 150.0,
    "descricao": "Almoco com cliente",
    "dataEfetivacao": "2026-03-21",
    "tipoPagamento": "Pix",
    "contaBancaria": "Conta principal",
    "cartao": null,
    "tipoDespesa": "Saude",
    "tipoReceita": null
  },
  {
    "idTransacao": 78,
    "tipoTransacao": "receita",
    "valor": 1200.0,
    "descricao": "Freelance",
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

## Contrato de resumo
### Query params
- `ano` (opcional, `int`)

### Regras
- quando `ano` for informado, o resumo considera apenas o ano recebido
- quando `ano` nao for informado, o resumo considera todo o historico do usuario autenticado
- `ano <= 0` retorna `ano_invalido`

### Campos de retorno
- `ano` (`null` quando o filtro nao for enviado)
- `totalReceitas`
- `totalDespesas`
- `totalReembolsos`
- `totalEstornos`
- `totalGeral` (soma dos quatro totais anteriores)

Observacao:
- os totais preservam o sinal original de `ValorTransacao` (nao aplicam valor absoluto)

### Exemplo de response de sucesso (200)
```json
{
  "ano": null,
  "totalReceitas": 12500.0,
  "totalDespesas": 8600.0,
  "totalReembolsos": 900.0,
  "totalEstornos": 240.0,
  "totalGeral": 22240.0
}
```

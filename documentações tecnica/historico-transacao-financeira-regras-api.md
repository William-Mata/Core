# HistoricoTransacaoFinanceiraController - Regras de API

## Rotas
- `GET /api/financeiro/historico-transacoes`
- `GET /api/financeiro/historico-transacoes/resumo`
- `GET /api/financeiro/historico-transacoes/resumo-por-ano`

## Regras principais
- Exige autenticacao JWT.
- Listagem valida `quantidadeRegistros > 0`.
- Listagem valida `ordemRegistros` em enum valido.
- Resumo aceita `ano` opcional; quando informado deve ser maior que zero.
- Resumo por ano exige `ano > 0`.
- Dados retornados sempre restritos ao usuario autenticado.
- Registros com `OcultarDoHistorico = true` sao desconsiderados nas consultas de historico.
- O filtro de ocultacao vale para:
  - listagem (`/historico-transacoes`)
  - resumo (`/historico-transacoes/resumo`)
  - resumo por ano (`/historico-transacoes/resumo-por-ano`)
  - consultas por conta/cartao usadas em outros fluxos financeiros.

## Integracao com efetivacao e estorno
- Efetivacoes podem gravar `observacaoHistorico` no historico.
- Estornos exigem `dataEstorno` no fluxo de origem e podem gravar `observacaoHistorico`.
- Estornos com `ocultarDoHistorico = true` marcam os registros da transacao para ocultacao no historico.

## Query params
- Listagem:
  - `quantidadeRegistros` (default 50)
  - `ordemRegistros` (default `MaisRecentes`)
- Resumo:
  - `ano` opcional
- Resumo por ano:
  - `ano` obrigatorio

## Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/HistoricoTransacaoFinanceiraController.cs`
- Service: `Core.Application/Services/Financeiro/HistoricoTransacaoFinanceiraConsultaService.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/HistoricoTransacaoFinanceiraRepository.cs`

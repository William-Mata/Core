# ReembolsoController - Regras de API

## Rotas
- `GET /api/financeiro/reembolsos`
- `GET /api/financeiro/reembolsos/{id}`
- `POST /api/financeiro/reembolsos`
- `PUT /api/financeiro/reembolsos/{id}`
- `DELETE /api/financeiro/reembolsos/{id}`
- `POST /api/financeiro/reembolsos/{id}/efetivar`
- `POST /api/financeiro/reembolsos/{id}/estornar`

## Regras principais
- Exige autenticacao JWT.
- Operacoes sao restritas ao usuario autenticado.
- `competencia` e a fonte de verdade para cadastro, edicao e listagem.
- `competencia` armazena apenas mes/ano no formato `yyyy-MM`.
- Quando `competencia` nao for informada, a API assume a competencia atual.
- Efetivacao e estorno obedecem as regras de status do reembolso.
- Exclusao retorna `204 No Content` quando sucesso.
- Efetivacao e estorno registram historico financeiro em `HistoricoTransacaoFinanceira`.
- Efetivacao aceita `observacaoHistorico` opcional e persiste no historico.
- Estorno exige `dataEstorno` e aceita `observacaoHistorico` opcional.
- Estorno aceita `ocultarDoHistorico` (default `true`) para ocultar registros da transacao no historico.

## Filtros de listagem
- `id`, `descricao`, `competencia`, `dataInicio`, `dataFim`
- Quando `competencia` for informada, a listagem filtra pela competencia normalizada e nao depende de `dataLancamento`.

## Contratos de efetivacao e estorno
- `POST /api/financeiro/reembolsos/{id}/efetivar`
  - Body: mesmo contrato anterior + `observacaoHistorico` (string opcional).
- `POST /api/financeiro/reembolsos/{id}/estornar`
  - Body:
    - `dataEstorno` (date, obrigatorio)
    - `observacaoHistorico` (string, opcional)
    - `ocultarDoHistorico` (bool, opcional, default `true`)

## Validacoes de fluxo (efetivacao/estorno)
- Efetivacao:
  - exige reembolso em status diferente de `Pago`.
  - `dataEfetivacao` nao pode ser menor que `dataLancamento`.
- Estorno:
  - exige reembolso em status `Pago`.
  - `dataEstorno` obrigatoria.
  - `dataEstorno` nao pode ser menor que `dataLancamento`.
  - quando houver `dataEfetivacao`, `dataEstorno` nao pode ser menor que `dataEfetivacao`.

## Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/ReembolsoController.cs`
- Service: `Core.Application/Services/Financeiro/ReembolsoService.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/ReembolsoRepository.cs`

# DespesaController - Regras de API

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

## Regras principais
- Exige autenticacao JWT.
- `valorLiquido = valorTotal - desconto + acrescimo + imposto + juros`.
- Criacao inicia em status `Pendente` e gera log.
- Edicao permitida somente para status pendente (conforme regra de service).
- Efetivacao muda status para `Efetivada`.
- Cancelamento e estorno respeitam regras de status.
- Efetivacao e estorno registram historico financeiro em `HistoricoTransacaoFinanceira`.
- Efetivacao aceita `observacaoHistorico` opcional e persiste no historico.
- Estorno exige `dataEstorno` e aceita `observacaoHistorico` opcional.
- Estorno aceita `ocultarDoHistorico` (default `true`) para ocultar registros da transacao no historico.
- Rateio por area/subarea:
  - area/subarea devem ser validas
  - subarea deve pertencer a area
  - area deve ser do tipo `Despesa`
  - soma dos rateios de area deve fechar com `valorTotal`
- Rateio por amigos:
  - amigos precisam ser validos/aceitos
  - soma precisa fechar com o valor informado para rateio

## Filtros de listagem
- `id`, `descricao`, `competencia`, `dataInicio`, `dataFim`, `verificarUltimaRecorrencia`

## Contratos de efetivacao e estorno
- `POST /api/financeiro/despesas/{id}/efetivar`
  - Body: mesmo contrato anterior + `observacaoHistorico` (string opcional).
- `POST /api/financeiro/despesas/{id}/estornar`
  - Body:
    - `dataEstorno` (date, obrigatorio)
    - `observacaoHistorico` (string, opcional)
    - `ocultarDoHistorico` (bool, opcional, default `true`)

## Validacoes de fluxo (efetivacao/estorno)
- Efetivacao:
  - exige despesa em status `Pendente`.
  - `dataEfetivacao` nao pode ser menor que `dataLancamento`.
- Estorno:
  - exige despesa em status `Efetivada`.
  - `dataEstorno` obrigatoria.
  - `dataEstorno` nao pode ser menor que `dataLancamento`.
  - quando houver `dataEfetivacao`, `dataEstorno` nao pode ser menor que `dataEfetivacao`.

## Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/DespesaController.cs`
- Service: `Core.Application/Services/Financeiro/DespesaService.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/DespesaRepository.cs`

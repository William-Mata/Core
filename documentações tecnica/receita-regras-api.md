# ReceitaController - Regras de API

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

## Regras principais
- Exige autenticacao JWT.
- `valorLiquido = valorTotal - desconto + acrescimo + imposto + juros`.
- Criacao inicia em status `Pendente` e gera log.
- Efetivacao muda status para `Efetivada`.
- Para `tipoRecebimento` que exige conta, `contaBancariaId` deve ser informado.
- Efetivacao e estorno registram historico financeiro em `HistoricoTransacaoFinanceira`.
- Efetivacao aceita `observacaoHistorico` opcional e persiste no historico.
- Estorno exige `dataEstorno` e aceita `observacaoHistorico` opcional.
- Estorno aceita `ocultarDoHistorico` (default `true`) para ocultar registros da transacao no historico.
- Rateio por area/subarea:
  - area/subarea devem ser validas
  - subarea deve pertencer a area
  - area deve ser do tipo `Receita`
  - soma dos rateios de area deve fechar com `valorTotal`
- Rateio por amigos:
  - amigos precisam ser validos/aceitos
  - soma precisa fechar com o valor informado para rateio

## Filtros de listagem
- `id`, `descricao`, `competencia`, `dataInicio`, `dataFim`, `verificarUltimaRecorrencia`

## Contratos de efetivacao e estorno
- `POST /api/financeiro/receitas/{id}/efetivar`
  - Body: mesmo contrato anterior + `observacaoHistorico` (string opcional).
- `POST /api/financeiro/receitas/{id}/estornar`
  - Body:
    - `dataEstorno` (date, obrigatorio)
    - `observacaoHistorico` (string, opcional)
    - `ocultarDoHistorico` (bool, opcional, default `true`)

## Validacoes de fluxo (efetivacao/estorno)
- Efetivacao:
  - exige receita em status `Pendente`.
  - `dataEfetivacao` nao pode ser menor que `dataLancamento`.
- Estorno:
  - exige receita em status `Efetivada`.
  - `dataEstorno` obrigatoria.
  - `dataEstorno` nao pode ser menor que `dataLancamento`.
  - quando houver `dataEfetivacao`, `dataEstorno` nao pode ser menor que `dataEfetivacao`.

## Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/ReceitaController.cs`
- Service: `Core.Application/Services/Financeiro/ReceitaService.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/ReceitaRepository.cs`

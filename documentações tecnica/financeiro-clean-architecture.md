# Backend Financeiro - Despesa, Receita, Conta Bancaria e Cartao

## Objetivo
Implementar contratos e regras das telas:
- `tela-despesa.md`
- `tela-receita.md`
- `tela-conta-bancaria.md`
- `tela-cartao.md`

seguindo Clean Architecture, SOLID e padrao de nomenclatura (tecnico em ingles e negocio em pt-BR).

## Endpoints implementados
- `GET /api/despesas`
- `GET /api/despesas/{id}`
- `POST /api/despesas`
- `PUT /api/despesas/{id}`
- `POST /api/despesas/{id}/efetivar`
- `POST /api/despesas/{id}/cancelar`
- `POST /api/despesas/{id}/estornar`

- `GET /api/receitas`
- `GET /api/receitas/{id}`
- `POST /api/receitas`
- `PUT /api/receitas/{id}`
- `POST /api/receitas/{id}/efetivar`
- `POST /api/receitas/{id}/cancelar`
- `POST /api/receitas/{id}/estornar`

- `GET /api/contas-bancarias`
- `GET /api/contas-bancarias/{id}`
- `POST /api/contas-bancarias`
- `PUT /api/contas-bancarias/{id}`
- `POST /api/contas-bancarias/{id}/inativar`
- `POST /api/contas-bancarias/{id}/ativar`

- `GET /api/cartoes`
- `GET /api/cartoes/{id}`
- `POST /api/cartoes`
- `PUT /api/cartoes/{id}`
- `POST /api/cartoes/{id}/inativar`
- `POST /api/cartoes/{id}/ativar`

## Regras de negocio aplicadas
- Despesa:
- calculo de `valorLiquido = valorTotal - desconto + acrescimo + imposto + juros`.
- criacao em `status = pendente` com log `CRIADA`.
- edicao permitida apenas em `pendente`.
- efetivacao define `status = efetivada` e `valorEfetivacao = valorLiquido`.
- cancelamento apenas em `pendente`.
- estorno apenas em `efetivada`.
- rateio por area/subarea e validado antes de persistir:
- se houver rateio, `AreaId` e `SubAreaId` devem ser maiores que zero.
- a subarea informada precisa existir e pertencer a area informada.
- a area vinculada ao rateio da despesa precisa ser do tipo `Despesa`.
- quando houver rateio por amigos, a soma dos valores deve ser igual ao `valorTotal`.
- quando houver rateio por areas, a soma dos valores deve ser igual ao `valorTotal`.

- Receita:
- mesmas regras base de liquido/status da despesa.
- `contaBancaria` obrigatoria quando `tipoRecebimento` for `pix` ou `transferencia` (criacao/edicao/efetivacao).
- rateio por area/subarea segue as mesmas validacoes estruturais da despesa, mas a area precisa ser do tipo `Receita`.
- quando houver rateio por amigos, a soma dos valores deve ser igual ao `valorTotal`.

- Conta bancaria:
- criacao com `status = ativa`, `saldoAtual = saldoInicial`, `extrato` vazio e log `CRIADA`.
- edicao preserva saldos.
- inativacao bloqueada quando `QuantidadePendencias > 0`.
- logs `ATIVADA` / `INATIVADA` em troca de status.

- Cartao:
- para `tipo = credito`, exige `limite`, `diaVencimento` e `dataVencimentoCartao`.
- para `tipo = debito`, limpa campos de vencimento e define `limite = 0`.
- cria com `status = ativo` e log `CRIADO`.
- inativacao bloqueada quando `QuantidadePendencias > 0`.

## Estrutura por camada
- `Core.Application`
- `Contracts`: interfaces de repositório por modulo.
- `Models`: contratos de dados para os 4 modulos.
- `UseCases`: orquestracao de regras de negocio e validacao.

- `Core.Infrastructure`
- repositorios em memoria para cada modulo financeiro.

- `Core.API`
- contratos de request em `Contracts/Financeiro`.
- controladores por modulo em `Controllers`.

## Testes criados
- Aplicacao:
- `Core.Tests/Application/DespesaUseCaseTests.cs`
- `Core.Tests/Application/ReceitaUseCaseTests.cs`
- `Core.Tests/Application/ContaBancariaUseCaseTests.cs`
- `Core.Tests/Application/CartaoUseCaseTests.cs`

- Integracao:
- `Core.Tests/Integration/DespesaControllerTests.cs`
- `Core.Tests/Integration/ReceitaControllerTests.cs`
- `Core.Tests/Integration/ContaBancariaControllerTests.cs`
- `Core.Tests/Integration/CartaoControllerTests.cs`

## Rateio na API financeira
Regra confirmada no backend:
- despesa aceita rateio com `amigosRateio` e/ou `areasRateio`.
- receita aceita rateio com `amigosRateio` e/ou `areasRateio`.
- a validacao de area/subarea bloqueia combinacoes inexistentes ou incompatíveis com o tipo financeiro da area.
- a validacao de soma dos rateios bloqueia valores que nao fecham com `valorTotal`.

### Exemplo valido - despesa

```json
{
  "descricao": "Almoco",
  "dataLancamento": "2026-03-10",
  "dataVencimento": "2026-03-10",
  "tipoDespesa": "alimentacao",
  "tipoPagamento": "pix",
  "recorrencia": "unica",
  "valorTotal": 100.0,
  "desconto": 0.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "amigosRateio": [
    { "nome": "Amigo 1", "valor": 40.0 },
    { "nome": "Amigo 2", "valor": 60.0 }
  ],
  "areasRateio": [
    { "areaId": 1, "subAreaId": 2, "valor": 100.0 }
  ]
}
```

### Exemplo invalido - receita

```json
{
  "descricao": "Servico",
  "dataLancamento": "2026-03-10",
  "dataVencimento": "2026-03-10",
  "tipoReceita": "freelance",
  "tipoRecebimento": "pix",
  "recorrencia": "unica",
  "valorTotal": 200.0,
  "desconto": 0.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "amigosRateio": [
    { "nome": "Amigo 1", "valor": 120.0 },
    { "nome": "Amigo 2", "valor": 50.0 }
  ],
  "areasRateio": [
    { "areaId": 1, "subAreaId": 2, "valor": 200.0 }
  ]
}
```

Neste caso a soma dos rateios por amigos nao fecha com `valorTotal`, e o backend tende a rejeitar a request com erro de rateio invalido.

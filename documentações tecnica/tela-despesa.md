# Tela de Despesa

## Objetivo
Documentar o contrato atual do front-end da tela de despesa para integracao com API.

Arquivo principal:
- `app/principal/financeiro/despesa.tsx`

## Rota do front
- listagem da tela: `/principal/financeiro/despesa`
- visualizacao por id: `/principal/financeiro/despesa?id={id}`

## Modos da tela
A tela trabalha com cinco modos internos:
- `lista`
- `novo`
- `edicao`
- `visualizacao`
- `efetivacao`

## Estrutura esperada de despesa
Campos usados pelo front:

```json
{
  "id": 1,
  "descricao": "Almoco com cliente",
  "observacao": "Reuniao comercial no centro.",
  "dataLancamento": "2026-03-10",
  "dataVencimento": "2026-03-15",
  "dataEfetivacao": "2026-03-15",
  "tipoDespesa": "alimentacao",
  "tipoPagamento": "pix",
  "recorrencia": "unica",
  "valorTotal": 150.0,
  "valorLiquido": 145.0,
  "desconto": 5.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "valorEfetivacao": 145.0,
  "status": "efetivada",
  "amigosRateio": ["Ana", "Bruno"],
  "tiposRateio": ["alimentacao"],
  "anexoDocumento": "recibo-almoco.pdf",
  "logs": [
    {
      "id": 1,
      "data": "2026-03-10",
      "acao": "CRIADA",
      "descricao": "Despesa criada com status pendente."
    }
  ]
}
```

## Enumeracoes esperadas pelo front

### Status
- `pendente`
- `efetivada`
- `cancelada`

### Tipo de despesa
- `alimentacao`
- `transporte`
- `moradia`
- `lazer`
- `saude`
- `educacao`
- `servicos`

### Tipo de pagamento
- `pix`
- `cartaoCredito`
- `cartaoDebito`
- `boleto`
- `transferencia`
- `dinheiro`

### Recorrencia
- `unica`
- `semanal`
- `mensal`
- `anual`

## Filtros da listagem
A tela usa filtro local com:
- `id`
- `descricao`
- `dataInicio`
- `dataFim`

Regras do front:
- `id` filtra por correspondencia parcial
- `descricao` filtra por descricao, observacao, tipo traduzido e status traduzido
- o intervalo de data usa `dataLancamento`

## Regras de validacao no cadastro e edicao
Campos obrigatorios exigidos pelo front:
- `descricao`
- `dataLancamento`
- `dataVencimento`
- `tipoDespesa`
- `tipoPagamento`
- `valorTotal`

Comportamento atual:
- quando um campo obrigatorio nao e preenchido, o front destaca o campo e exibe alerta
- o calculo de `valorLiquido` e automatico
- `valorLiquido` e bloqueado na UI
- formula atual:

```txt
valorLiquido = valorTotal - desconto + acrescimo + imposto + juros
```

## Regras de cadastro
Ao salvar uma nova despesa o front:
- gera o `id` localmente no mock atual
- define `status = pendente`
- grava `logs` com acao `CRIADA`

Payload recomendado para criacao:

```json
{
  "descricao": "Almoco com cliente",
  "observacao": "Reuniao comercial no centro.",
  "dataLancamento": "2026-03-10",
  "dataVencimento": "2026-03-15",
  "tipoDespesa": "alimentacao",
  "tipoPagamento": "pix",
  "recorrencia": "unica",
  "valorTotal": 150.0,
  "desconto": 5.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "amigosRateio": ["Ana", "Bruno"],
  "tiposRateio": ["alimentacao"],
  "anexoDocumento": "recibo-almoco.pdf"
}
```

## Regras de edicao
- o front so permite editar despesa com `status = pendente`
- ao editar, grava log com acao `EDITADA`
- a mesma validacao do cadastro continua valendo

## Regras de visualizacao
A visualizacao mostra:
- todos os campos principais
- status
- data de efetivacao
- logs de alteracao

## Regras de efetivacao
Campos exigidos pelo front:
- `dataEfetivacao`
- `tipoPagamento`
- `valorTotal`

Comportamento atual:
- `valorLiquido` fica bloqueado
- `valorEfetivacao` fica bloqueado
- `valorEfetivacao` recebe o mesmo valor do `valorLiquido`
- ao efetivar, o front define:
  - `status = efetivada`
  - `valorEfetivacao = valorLiquido`
  - log com acao `EFETIVADA`

Payload recomendado para efetivacao:

```json
{
  "dataEfetivacao": "2026-03-15",
  "tipoPagamento": "pix",
  "valorTotal": 150.0,
  "desconto": 5.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "anexoDocumento": "recibo-almoco.pdf"
}
```

## Regras de cancelamento
- o front so permite cancelar despesa com `status = pendente`
- na web, confirma via `confirm()` nativo
- no mobile, confirma via `Alert.alert`
- ao cancelar, o front define:
  - `status = cancelada`
  - log com acao `CANCELADA`

## Regras de estorno
- o front so permite estornar despesa com `status = efetivada`
- ao estornar, o front define:
  - `status = pendente`
  - `dataEfetivacao = undefined`
  - `valorEfetivacao = undefined`
  - log com acao `ESTORNADA`

## Regras de upload
- `anexoDocumento` e tratado como seletor de arquivo
- o front espera receber ou manter o nome do arquivo para exibicao

## Regras de formatacao
- valores devem ser enviados como numero
- datas devem ser enviadas em ISO `yyyy-MM-dd`
- o front aplica formatacao por idioma para exibicao
- os campos monetarios usam mascara local no cliente

## Fora do escopo atual da tela
- rateio com percentuais
- persistencia real dos uploads
- auditoria de usuario que alterou cada log

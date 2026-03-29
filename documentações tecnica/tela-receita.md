# Tela de Receita

## Objetivo
Documentar o contrato atual do front-end da tela de receita para integracao com API.

Arquivo principal:
- `app/principal/financeiro/receita.tsx`

## Rota do front
- listagem da tela: `/principal/financeiro/receita`
- visualizacao por id: `/principal/financeiro/receita?id={id}`

## Modos da tela
A tela trabalha com cinco modos internos:
- `lista`
- `novo`
- `edicao`
- `visualizacao`
- `efetivacao`

## Estrutura esperada de receita
Campos usados pelo front:

```json
{
  "id": 1,
  "descricao": "Salario mensal",
  "observacao": "Recebimento principal do mes.",
  "dataLancamento": "2026-03-01",
  "dataVencimento": "2026-03-05",
  "dataEfetivacao": "2026-03-05",
  "tipoReceita": "salario",
  "tipoRecebimento": "transferencia",
  "recorrencia": "mensal",
  "valorTotal": 5800.0,
  "valorLiquido": 5650.0,
  "desconto": 150.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "valorEfetivacao": 5650.0,
  "status": "efetivada",
  "amigosRateio": [],
  "rateioAmigosValores": {},
  "areasRateio": ["Comercial > Projeto A"],
  "rateioAreasValores": {
    "Comercial > Projeto A": 5650.0
  },
  "contaBancaria": "Conta Principal",
  "anexoDocumento": "holerite-marco.pdf",
  "logs": []
}
```

## Enumeracoes esperadas pelo front

### Status
- `pendente`
- `efetivada`
- `cancelada`

### Tipo de receita
- `salario`
- `freelance`
- `reembolso`
- `investimento`
- `bonus`
- `outros`

### Tipo de recebimento
- `pix`
- `transferencia`
- `contaCorrente`
- `dinheiro`
- `boleto`

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
- `tipoReceita`
- `tipoRecebimento`
- `valorTotal`

Regra adicional:
- `contaBancaria` passa a ser obrigatoria quando `tipoRecebimento` for `pix` ou `transferencia`

Comportamento atual:
- quando um campo obrigatorio nao e preenchido, o front destaca o campo e exibe alerta
- o calculo de `valorLiquido` e automatico
- `valorLiquido` e bloqueado na UI
- formula atual:

```txt
valorLiquido = valorTotal - desconto + acrescimo + imposto + juros
```

## Regras de cadastro
Ao salvar uma nova receita o front:
- gera o `id` localmente no mock atual
- define `status = pendente`
- grava `logs` com acao `CRIADA`

Payload recomendado para criacao:

```json
{
  "descricao": "Freelance design",
  "observacao": "Projeto pontual.",
  "dataLancamento": "2026-03-12",
  "dataVencimento": "2026-03-20",
  "tipoReceita": "freelance",
  "tipoRecebimento": "pix",
  "recorrencia": "unica",
  "valorTotal": 1200.0,
  "desconto": 0.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "amigosRateio": ["Ana"],
  "rateioAmigosValores": {
    "Ana": 400.0
  },
  "areasRateio": ["Marketing > Midia"],
  "rateioAreasValores": {
    "Marketing > Midia": 800.0
  },
  "contaBancaria": "Conta Principal",
  "anexoDocumento": "proposta.pdf"
}
```

## Regras de edicao
- o front so permite editar receita com `status = pendente`
- ao editar, grava log com acao `EDITADA`
- a mesma validacao do cadastro continua valendo

## Regras de visualizacao
A visualizacao mostra:
- todos os campos principais
- status
- data de efetivacao
- logs de alteracao
- rateios com valores por amigo e por area / subarea

## Regras de efetivacao
Campos exigidos pelo front:
- `dataEfetivacao`
- `tipoRecebimento`
- `valorTotal`

Regra adicional na efetivacao:
- `contaBancaria` e obrigatoria quando `tipoRecebimento` for `pix` ou `transferencia`

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
  "dataEfetivacao": "2026-03-20",
  "tipoRecebimento": "pix",
  "contaBancaria": "Conta Principal",
  "valorTotal": 1200.0,
  "desconto": 0.0,
  "acrescimo": 0.0,
  "imposto": 0.0,
  "juros": 0.0,
  "anexoDocumento": "comprovante.pdf"
}
```

## Regras de cancelamento
- o front so permite cancelar receita com `status = pendente`
- confirma cancelamento via alerta nativo
- ao cancelar, o front define:
  - `status = cancelada`
  - log com acao `CANCELADA`

## Regras de estorno
- o front so permite estornar receita com `status = efetivada`
- ao estornar, o front define:
  - `status = pendente`
  - `dataEfetivacao = undefined`
  - `valorEfetivacao = undefined`
  - log com acao `ESTORNADA`

## Regras de rateio
O front suporta dois blocos de rateio:
- `amigosRateio` com `rateioAmigosValores`
- `areasRateio` com `rateioAreasValores`

Regra atual:
- o front nao valida se a soma dos rateios bate com o valor liquido
- ele apenas captura os valores informados e envia / armazena no estado

## Regras de upload
- `anexoDocumento` e tratado como seletor de arquivo
- o front espera receber ou manter o nome do arquivo para exibicao

## Regras de formatacao
- valores devem ser enviados como numero
- datas devem ser enviadas em ISO `yyyy-MM-dd`
- o front aplica formatacao por idioma para exibicao
- os campos monetarios usam mascara local no cliente

## Fora do escopo atual da tela
- validacao automatica do fechamento dos rateios
- persistencia real dos uploads
- conciliacao automatica com conta bancaria

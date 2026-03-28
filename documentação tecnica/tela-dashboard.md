# Tela de Dashboard

## Objetivo
Documentar o comportamento atual do front-end da dashboard e o contrato de dados necessario para integrar a tela com API.

Arquivo principal:
- `app/principal/index.tsx`

## Rota do front
- tela principal da dashboard: `/principal`
- documentacao do modulo: `/principal/documentacao`

## Estrategia atual da tela
No estado atual do front, a dashboard monta os widgets a partir de:
- lista de transacoes efetivadas
- lista de contas e cartoes com saldo

Ou seja, a integracao mais aderente ao comportamento atual pode seguir dois caminhos:
1. retornar tudo consolidado em um endpoint unico da dashboard
2. retornar transacoes e saldos, deixando o front calcular os widgets

Como a tela atual ja calcula os widgets localmente, o contrato mais aderente hoje e o segundo.

## Contrato recomendado para a API
Endpoint sugerido:
- `GET /api/dashboard`

Resposta recomendada:

```json
{
  "transacoes": [
    {
      "id": 1001,
      "tipo": "despesa",
      "valor": 145.00,
      "descricao": "Almoco com cliente",
      "dataEfetivacao": "2026-03-15",
      "codigoPagamento": "CARTAO_CREDITO",
      "tipoPagamento": "Cartao de credito",
      "contaBancaria": "Itau",
      "cartao": "Visa Platinum",
      "area": "Operacoes",
      "subarea": "Suprimentos"
    }
  ],
  "balanco": [
    {
      "id": "conta-1",
      "tipo": "conta",
      "nome": "Itau",
      "subtitulo": "Saldo atual da conta",
      "saldo": 8650.42
    },
    {
      "id": "cartao-1",
      "tipo": "cartao",
      "nome": "Visa Platinum",
      "subtitulo": "Saldo disponivel do cartao",
      "saldo": 3870.90
    }
  ]
}
```

## Estrutura esperada de transacao
Campos esperados pelo front:
- `id`: numero da transacao
- `tipo`: um dos valores:
  - `despesa`
  - `receita`
  - `reembolso`
  - `estorno`
- `valor`: numero decimal
- `descricao`: texto exibivel ja pronto para UI
- `dataEfetivacao`: data ISO `yyyy-MM-dd`
- `codigoPagamento`: codigo tecnico usado para regra de exibicao
- `tipoPagamento`: texto exibivel ja pronto para UI
- `contaBancaria`: texto exibivel opcional
- `cartao`: texto exibivel opcional
- `area`: texto exibivel da area
- `subarea`: texto exibivel da subarea

## Regras do front para pagamento e cartao
- a coluna `Cartao` da widget `Ultimas Transacoes` so deve exibir valor quando `codigoPagamento = CARTAO_CREDITO`
- para outros tipos de pagamento / recebimento, a coluna deve exibir `-`
- `tipoPagamento` e exibido como texto diretamente na grid

## Estrutura esperada de balanco
Campos esperados:
- `id`
- `tipo`: `conta` ou `cartao`
- `nome`
- `subtitulo`
- `saldo`

Regras do front:
- `tipo = conta` usa a traducao de conta bancaria no card
- `tipo = cartao` usa a traducao de cartao no card
- saldo positivo e destacado como sucesso
- saldo negativo e destacado como erro

## Widgets e regras atuais da dashboard

### 1. Resumo Financeiro
Calculado pelo front a partir de todas as transacoes carregadas.

Valores exibidos:
- total de receitas
- total de despesas
- total de reembolsos
- total de estornos
- saldo

Formula do saldo atual:

```txt
saldo = receitas + reembolsos + estornos - despesas
```

### 2. Grafico por Area e Subarea - Receitas
Calculado pelo front a partir das transacoes com `tipo = receita`.

Regras:
- agrupa por `area + subarea`
- ordena por maior volume total
- exibe no maximo 30 combinacoes
- o grafico usa `PieChart`
- a lista lateral e apenas visual no estado atual

### 3. Grafico por Area e Subarea - Despesas
Calculado pelo front a partir das transacoes com `tipo = despesa`.

Regras:
- agrupa por `area + subarea`
- ordena por maior volume total
- exibe no maximo 30 combinacoes
- o grafico usa `PieChart`
- a lista lateral e apenas visual no estado atual

### 4. Grafico Anual
Calculado pelo front a partir das transacoes do ano atual.

Series exibidas:
- receitas
- despesas
- reembolsos
- estornos

Regras:
- o grafico ocupa toda a largura util da widget
- o usuario pode ativar e desativar series individualmente
- o front nao permite desligar todas as series ao mesmo tempo
- o tooltip do ponto usa os dados do mes selecionado

### 5. Ultimas Transacoes
Regras atuais:
- exibe no maximo 100 transacoes
- ordenacao atual: data mais recente primeiro
- colunas atuais:
  - `ID`
  - `Tipo`
  - `Valor`
  - `Descricao`
  - `Data`
  - `Pagamento / Recebimento`
  - `Conta Bancaria`
  - `Cartao`
  - `Area / Subarea`

### 6. Balanco Geral
Regras atuais:
- exibe contas e cartoes no mesmo bloco
- cada item precisa trazer seu saldo pronto
- o front nao recalcula saldo a partir das transacoes dentro desta widget

## Reordenacao de widgets
A dashboard permite reordenacao de widgets por dois meios:
- botoes de mover para cima e para baixo
- drag and drop na web

Regras do front:
- a ordem e mantida apenas em memoria na tela no estado atual
- ainda nao existe persistencia em API da ordenacao do usuario

## Regras de formatacao
- valores devem chegar como numero e o front aplica formatacao por idioma
- datas devem chegar em ISO `yyyy-MM-dd` e o front aplica formatacao por idioma
- a tela suporta `pt-BR`, `en` e `es`

## Regras importantes para integracao
- evitar enviar valores monetarios formatados como string
- evitar enviar datas em formato local como `24/03/2026`
- `descricao`, `tipoPagamento`, `area` e `subarea` podem vir como texto final de exibicao
- se optar por enviar codigos em vez de texto final, o front precisara de dicionario de traducao adicional
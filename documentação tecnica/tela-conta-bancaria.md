# Tela de Conta Bancaria

## Objetivo
Documentar o contrato atual do front-end da tela de conta bancaria para integracao com API.

Arquivo principal:
- `app/principal/financeiro/conta-bancaria.tsx`

## Rota do front
- tela: `/principal/financeiro/conta-bancaria`

## Modos da tela
A tela trabalha com quatro modos internos:
- `lista`
- `novo`
- `edicao`
- `visualizacao`

## Estrutura esperada de conta bancaria
Campos usados pelo front:

```json
{
  "id": 1,
  "descricao": "Conta Principal",
  "banco": "Itau",
  "agencia": "1245",
  "numero": "55667-8",
  "saldoInicial": 3200.0,
  "saldoAtual": 4850.1,
  "dataAbertura": "2026-01-15",
  "status": "ativa",
  "extrato": [
    {
      "id": 1,
      "data": "2026-03-02",
      "descricao": "Recebimento projeto",
      "tipo": "credito",
      "valor": 1800.0
    }
  ],
  "logs": [
    {
      "id": 1,
      "data": "2026-01-15",
      "acao": "CRIADA",
      "descricao": "Conta bancaria criada com status ativa."
    }
  ]
}
```

## Enumeracoes esperadas pelo front

### Status
- `ativa`
- `inativa`

### Tipo do movimento de extrato
- `credito`
- `debito`

## Filtros da listagem
A tela usa filtro local com:
- `id`
- `descricao`
- `dataInicio`
- `dataFim`

Regras do front:
- `id` filtra por correspondencia parcial
- `descricao` filtra por descricao, banco, numero e status traduzido
- o intervalo de data usa `dataAbertura`

## Regras de validacao no cadastro e edicao
Campos obrigatorios exigidos pelo front:
- `descricao`
- `banco`
- `agencia`
- `numero`
- `dataAbertura`
- `saldoInicial`

Regras de saldo:
- no modo `novo`, `saldoInicial` e digitavel
- ao digitar `saldoInicial`, o front replica o mesmo valor em `saldoAtual`
- no modo `edicao`, `saldoInicial` fica bloqueado
- no modo `edicao`, `saldoAtual` tambem fica bloqueado
- ao editar, o front preserva `saldoAtual` do registro

## Regras de cadastro
Ao salvar uma nova conta o front:
- gera `id` localmente no mock atual
- define `status = ativa`
- inicializa `extrato` vazio
- cria `logs` com acao `CRIADA`

Payload recomendado para criacao:

```json
{
  "descricao": "Conta Principal",
  "banco": "Itau",
  "agencia": "1245",
  "numero": "55667-8",
  "saldoInicial": 3200.0,
  "dataAbertura": "2026-01-15"
}
```

## Regras de edicao
- a tela permite editar contas independentemente do status atual
- ao editar, grava log com acao `ATUALIZADA`
- `saldoInicial` e `saldoAtual` ficam bloqueados na UI durante a edicao

## Regras de visualizacao
A visualizacao mostra:
- todos os campos da conta
- status
- logs de alteracao

## Regra de ativacao e inativacao
- `Inativar` so aparece quando `status = ativa`
- `Ativar` so aparece quando `status = inativa`

Validacao para inativar:
- o front consulta um mapa local de pendencias por descricao da conta
- se existir quantidade pendente maior que zero, bloqueia a inativacao

Comportamento atual:
- web usa `confirm()` nativo
- mobile usa `Alert.alert`
- ao alternar status, grava log com `ATIVADA` ou `INATIVADA`

## Estrutura esperada do extrato
Campos usados pelo front:
- `id`
- `data`
- `descricao`
- `tipo`
- `valor`

Comportamento atual:
- o extrato e aberto dentro do card da listagem
- nao existe navegacao mensal no extrato da conta
- cada movimento mostra sinal visual por tipo:
  - `credito` em positivo
  - `debito` em negativo

## Regras de formatacao
- valores devem ser enviados como numero
- datas devem ser enviadas em ISO `yyyy-MM-dd`
- o front aplica formatacao por idioma para exibicao
- os campos monetarios usam mascara local no cliente

## Regras importantes para integracao
- `banco` hoje e texto, nao id numerico
- a API pode retornar o banco como texto exibivel igual ao usado pelo cadastro
- para suportar o seletor atual, o texto do banco precisa ser compativel com a lista exibida no front

## Fora do escopo atual da tela
- conciliacao automatica de saldo atual com base nos movimentos
- navegacao mensal do extrato
- validacao real de pendencias via backend em tempo real

# Tela de Cartao

## Objetivo
Documentar o contrato atual do front-end da tela de cartao para integracao com API.

Arquivo principal:
- `app/principal/financeiro/cartao.tsx`

## Rota do front
- tela: `/principal/financeiro/cartao`

## Modos da tela
A tela trabalha com quatro modos internos:
- `lista`
- `novo`
- `edicao`
- `visualizacao`

## Estrutura esperada de cartao
Campos usados pelo front:

```json
{
  "id": 1,
  "descricao": "Nubank Gold",
  "bandeira": "Mastercard",
  "tipo": "credito",
  "limite": 5000.0,
  "saldoDisponivel": 3280.45,
  "diaVencimento": "2026-03-12",
  "dataVencimentoCartao": "2026-03-12",
  "status": "ativo",
  "lancamentos": [
    {
      "id": 1,
      "data": "2026-03-05",
      "descricao": "Supermercado",
      "valor": 320.6
    }
  ],
  "logs": [
    {
      "id": 1,
      "data": "2026-01-10",
      "acao": "CRIADO",
      "descricao": "Cartao criado com status ativo."
    }
  ]
}
```

## Enumeracoes esperadas pelo front

### Tipo
- `credito`
- `debito`

### Status
- `ativo`
- `inativo`

## Regras condicionais por tipo

### Cartao de credito
Campos usados na UI:
- `limite`
- `saldoDisponivel`
- `diaVencimento`
- `dataVencimentoCartao`
- detalhe por `fatura`

### Cartao de debito
Campos usados na UI:
- `saldoDisponivel`
- detalhe por `extrato`

Campos ocultados na UI para debito:
- `limite`
- `diaVencimento`
- `dataVencimentoCartao`

## Filtros da listagem
A tela usa filtro local com:
- `id`
- `descricao`
- `dataInicio`
- `dataFim`

Regras do front:
- `id` filtra por correspondencia parcial
- `descricao` filtra por descricao, bandeira, tipo traduzido e status traduzido
- a base de data do filtro e `dataVencimentoCartao` quando existir; caso contrario usa a primeira data de log

## Regras de validacao no cadastro e edicao
Campos obrigatorios comuns:
- `descricao`
- `bandeira`
- `tipo`

Campos obrigatorios para cartao de credito:
- `limite`
- `diaVencimento`
- `dataVencimentoCartao`

Campos obrigatorios para cartao de debito:
- nao ha campos extras alem dos comuns

Regra de saldo disponivel:
- no modo `novo`, `saldoDisponivel` e digitavel
- no modo `edicao`, `saldoDisponivel` fica bloqueado e o front preserva o valor atual do registro

## Regras de cadastro
Ao salvar um novo cartao o front:
- gera `id` localmente no mock atual
- define `status = ativo`
- cria `logs` com acao `CRIADO`
- inicializa `lancamentos` como lista vazia

Payload recomendado para criacao:

### Credito
```json
{
  "descricao": "Cartao corporativo",
  "bandeira": "Visa",
  "tipo": "credito",
  "limite": 8000.0,
  "saldoDisponivel": 8000.0,
  "diaVencimento": "2026-03-12",
  "dataVencimentoCartao": "2026-03-12"
}
```

### Debito
```json
{
  "descricao": "Cartao operacional",
  "bandeira": "Mastercard",
  "tipo": "debito",
  "saldoDisponivel": 1500.0
}
```

## Regras de edicao
- a tela permite editar cartoes independentemente do status atual
- ao editar, grava log com acao `ATUALIZADO`
- `saldoDisponivel` continua bloqueado na UI durante a edicao
- quando o tipo passa de `credito` para `debito`, o front limpa os campos de vencimento e zera `limite`

## Regras de visualizacao
A visualizacao mostra:
- todos os campos aplicaveis ao tipo do cartao
- status
- logs de alteracao

## Regra de ativacao e inativacao
- `Inativar` so aparece quando `status = ativo`
- `Ativar` so aparece quando `status = inativo`

Validacao para inativar:
- o front consulta um mapa local de pendencias por descricao do cartao
- se existir quantidade pendente maior que zero, bloqueia a inativacao

Comportamento atual:
- web usa `confirm()` nativo
- mobile usa `Alert.alert`
- ao alternar status, grava log com `ATIVADO` ou `INATIVADO`

## Estrutura esperada de lancamentos
Campos usados pelo front:
- `id`
- `data`
- `descricao`
- `valor`

Regra atual:
- a tela nao armazena tipo do lancamento do cartao
- o valor e sempre exibido como gasto no detalhe do cartao

## Regra de fatura e extrato
- para `credito`, a acao lateral exibe `fatura`
- para `debito`, a acao lateral exibe `extrato`

Comportamento atual:
- ha navegacao mensal por `YYYY-MM`
- o detalhe mostra apenas os `lancamentos` do mes selecionado
- o total do periodo e a soma dos lancamentos filtrados pelo mes

## Regras de formatacao
- valores devem ser enviados como numero
- datas devem ser enviadas em ISO `yyyy-MM-dd`
- o front aplica formatacao por idioma para exibicao
- os campos monetarios usam mascara local no cliente

## Regras importantes para integracao
- `bandeira` hoje e texto, nao id numerico
- a API pode retornar a bandeira como texto exibivel igual ao usado pelo cadastro
- para suportar detalhe mensal corretamente, `lancamentos` devem trazer datas consistentes em ISO

## Fora do escopo atual da tela
- vinculacao real do cartao de debito com conta bancaria
- persistencia da navegacao mensal
- validacao real de pendencias via backend em tempo real

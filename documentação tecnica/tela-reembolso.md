# Tela de Reembolso

## Objetivo
Documentar o contrato esperado pelo front-end para a tela de reembolso.

Arquivo principal:
- `app/principal/financeiro/reembolso.tsx`

## Rota da tela
- `/principal/financeiro/reembolso`

## Endpoints consumidos
- `GET /api/financeiro/reembolsos`
- `GET /api/financeiro/despesas`
- `POST /api/financeiro/reembolsos`
- `PUT /api/financeiro/reembolsos/{id}`
- `DELETE /api/financeiro/reembolsos/{id}`

## Estrutura esperada de Reembolso (resposta)
Campos lidos pelo front:

```json
{
  "id": 1,
  "descricao": "Viagem comercial - semana 2",
  "solicitante": "João Silva",
  "dataSolicitacao": "2026-03-18",
  "despesasVinculadas": [1, 3],
  "status": "AGUARDANDO"
}
```

Observações:
- `status` pode vir em maiúsculo/minúsculo.
- Também são aceitas variações como `solicitanteName`.
- `despesasVinculadas` pode vir como array de ids (`[1,2]`) ou objetos com `id`.

## Estrutura esperada de Despesa (para seleção)
Campos mínimos:

```json
{
  "id": 10,
  "titulo": "Combustível viagem",
  "valor": 185.00,
  "data": "2026-03-15"
}
```

## Payload de criação/edição enviado pelo front

```json
{
  "descricao": "Viagem comercial - semana 2",
  "solicitante": "João Silva",
  "dataSolicitacao": "2026-03-18",
  "despesasVinculadas": [1, 3],
  "valorTotal": 274.9,
  "status": "AGUARDANDO"
}
```

## Regras de validação no front
- `descricao` obrigatória.
- `despesasVinculadas` deve conter pelo menos 1 item.
- Regra de unicidade:
  - uma despesa só pode estar vinculada a um único reembolso.
  - ao editar, o próprio reembolso é ignorado na checagem.
  - em conflito, o front bloqueia o save e mostra erro.

## Regras de cálculo
- `valorTotal` é calculado no front somando o valor das despesas selecionadas.
- `valorTotal` não é campo digitável.

## Filtro da listagem
Campos:
- `id`
- `descricao`
- `dataInicio`
- `dataFim`

Regras:
- `id`: correspondência parcial por texto.
- `descricao`: busca por `descricao` e `solicitante`.
- período: aplicado sobre `dataSolicitacao`.

## Tratamento de erro
- Erros de API são exibidos via notificação usando o parser padrão RFC 7807 do projeto.
- Mensagens de fallback:
  - `financeiro.reembolso.mensagens.falhaCarregar`
  - `financeiro.reembolso.mensagens.falhaSalvar`

## Observação de integração
- O front não usa mais dados mockados locais nesta tela.
- A API precisa retornar despesas elegíveis em `GET /financeiro/despesas` para o seletor múltiplo funcionar.


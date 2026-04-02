# Cartao - Regras de API

## 1. Contexto

- Modulo: Financeiro
- Controller: CartaoController
- Escopo desta documentacao: cadastrar, obter por id e editar cartao

## 2. Contrato de consumo

### 2.1 POST /api/financeiro/cartoes (Cadastrar)

- Metodo HTTP: POST
- Rota: /api/financeiro/cartoes
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado

#### Request

Headers:
- Authorization: Bearer <token>
- Content-Type: application/json

Body:

```json
{
  "descricao": "Cartao principal",
  "bandeira": "Visa",
  "tipo": "Credito",
  "limite": 5000.00,
  "saldoDisponivel": 5000.00,
  "diaVencimento": "2026-04-10",
  "dataVencimentoCartao": "2026-04-25"
}
```

#### Response de sucesso

- Status code: 201 Created
- Body: CartaoDto

```json
{
  "id": 20,
  "descricao": "Cartao principal",
  "bandeira": "Visa",
  "tipo": "Credito",
  "limite": 5000.00,
  "saldoDisponivel": 5000.00,
  "diaVencimento": "2026-04-10",
  "dataVencimentoCartao": "2026-04-25",
  "status": "ativo",
  "lancamentos": [],
  "logs": [
    {
      "id": 1,
      "data": "2026-04-01",
      "acao": "Cadastro",
      "descricao": "Cartao criado com status ativo."
    }
  ]
}
```

### 2.2 GET /api/financeiro/cartoes/{id} (Obter por id)

- Metodo HTTP: GET
- Rota: /api/financeiro/cartoes/{id}
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado e dono do recurso

#### Request

Path params:
- id (long)

Headers:
- Authorization: Bearer <token>

#### Response de sucesso

- Status code: 200 OK
- Body: CartaoDto

```json
{
  "id": 20,
  "descricao": "Cartao principal",
  "bandeira": "Visa",
  "tipo": "Credito",
  "limite": 5000.00,
  "saldoDisponivel": 4200.00,
  "diaVencimento": "2026-04-10",
  "dataVencimentoCartao": "2026-04-25",
  "status": "ativo",
  "lancamentos": [
    {
      "id": 90,
      "data": "2026-04-03",
      "descricao": "Compra supermercado",
      "valor": 300.00
    }
  ],
  "logs": [
    {
      "id": 1,
      "data": "2026-04-01",
      "acao": "Cadastro",
      "descricao": "Cartao criado com status ativo."
    }
  ]
}
```

### 2.3 PUT /api/financeiro/cartoes/{id} (Editar)

- Metodo HTTP: PUT
- Rota: /api/financeiro/cartoes/{id}
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado e dono do recurso

#### Request

Path params:
- id (long)

Headers:
- Authorization: Bearer <token>
- Content-Type: application/json

Body:

```json
{
  "descricao": "Cartao viagens",
  "bandeira": "Mastercard",
  "tipo": "Credito",
  "limite": 7000.00,
  "diaVencimento": "2026-04-12",
  "dataVencimentoCartao": "2026-04-27"
}
```

#### Response de sucesso

- Status code: 200 OK
- Body: CartaoDto (atualizado)

```json
{
  "id": 20,
  "descricao": "Cartao viagens",
  "bandeira": "Mastercard",
  "tipo": "Credito",
  "limite": 7000.00,
  "saldoDisponivel": 4200.00,
  "diaVencimento": "2026-04-12",
  "dataVencimentoCartao": "2026-04-27",
  "status": "ativo",
  "lancamentos": [],
  "logs": [
    {
      "id": 2,
      "data": "2026-04-04",
      "acao": "Atualizacao",
      "descricao": "Cartao atualizado."
    }
  ]
}
```

## 3. Regras aplicadas

### 3.1 Validacoes de entrada

Fatos confirmados:
- `descricao` e `bandeira` sao obrigatorios em cadastro e edicao; vazio/whitespace retorna `campo_obrigatorio`.
- `tipo` deve ser enum valido (`TipoCartao`); senao retorna `tipo_invalido`.
- `saldoDisponivel` nao pode ser negativo no cadastro; senao `saldo_invalido`.
- Se `tipo = Credito`, exige:
  - `limite` > 0
  - `diaVencimento` informado
  - `dataVencimentoCartao` informado
  senao retorna `dados_credito_obrigatorios`.
- `id` deve ser `long` pela rota `{id:long}`.

### 3.2 Regras de negocio

Fatos confirmados:
- Todas as operacoes dependem de usuario autenticado.
- Obter por id e editar filtram por `id` + `usuario autenticado` no repositorio.
- Se nao encontrar cartao no escopo do usuario, retorna `cartao_nao_encontrado`.
- No cadastro:
  - `Status = Ativo`
  - Se tipo nao for credito, `limite = 0`, `diaVencimento = null`, `dataVencimentoCartao = null`.
- Na edicao, para validar saldo, o service usa o `saldoDisponivel` ja persistido no cartao (nao vem no payload de update).

### 3.3 Efeitos colaterais

Fatos confirmados:
- Persistencia:
  - POST cria cartao.
  - PUT atualiza cartao.
- Logs:
  - POST adiciona log de cadastro.
  - PUT adiciona log de atualizacao.
- Integracoes/eventos:
  - Nao ha publicacao de evento externo nestes endpoints.

## 4. Erros e cenarios de falha

| Status | Condicao | Mensagem/retorno |
|---|---|---|
| 400 | Campos obrigatorios ausentes | `code: "campo_obrigatorio"` |
| 400 | Tipo de cartao invalido | `code: "tipo_invalido"` |
| 400 | Saldo disponivel negativo (POST) | `code: "saldo_invalido"` |
| 400 | Dados obrigatorios de credito ausentes | `code: "dados_credito_obrigatorios"` |
| 401 | Token ausente/invalido | ProblemDetails de nao autorizado |
| 404 | Cartao nao encontrado para usuario autenticado (GET/PUT) | `code: "cartao_nao_encontrado"` |
| 500 | Erro nao tratado | `code: "erro_interno"` |

## 5. Exemplos de consumo

### 5.1 Exemplo valido (Editar)

```bash
curl -X PUT "https://api.exemplo.com/api/financeiro/cartoes/20" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "descricao": "Cartao viagens",
    "bandeira": "Mastercard",
    "tipo": "Credito",
    "limite": 7000.00,
    "diaVencimento": "2026-04-12",
    "dataVencimentoCartao": "2026-04-27"
  }'
```

### 5.2 Exemplo com erro esperado (Cadastrar credito sem limite)

```bash
curl -X POST "https://api.exemplo.com/api/financeiro/cartoes" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "descricao": "Cartao sem limite",
    "bandeira": "Visa",
    "tipo": "Credito",
    "limite": 0,
    "saldoDisponivel": 5000.00,
    "diaVencimento": null,
    "dataVencimentoCartao": null
  }'
```

Resposta esperada (400):

```json
{
  "status": 400,
  "title": "Requisicao invalida",
  "detail": "Informe limite e datas obrigatorias para cartao de credito.",
  "code": "dados_credito_obrigatorios"
}
```

## 6. Rastreabilidade no codigo

- Controller/rota: `Core.Api/Controllers/Financeiro/CartaoController.cs`
- Service/use case: `Core.Application/Services/Financeiro/CartaoService.cs`
- Validator/schema: validacoes inline no service
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/CartaoRepository.cs`
- Excecoes/enums: `DomainException`, `NotFoundException`, `TipoCartao`, `StatusCartao`
- Tratamento de erro HTTP: `Core.Api/Middlewares/ErrorHandlingMiddleware.cs` e `Core.Api/Extensions/ErroMensagemExtensions.cs`
- Testes relacionados: nao identificados nesta analise

## 7. Pendencias

- Pendencia: nao foi localizado teste automatizado especifico para POST/GET por id/PUT de cartao.
- Impacto: risco de regressao em validacoes de tipo de cartao e cenarios de credito.
- Acao sugerida: criar testes de integracao para sucesso e falha (400/401/404).

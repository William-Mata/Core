# Conta Bancaria - Regras de API

## 1. Contexto

- Modulo: Financeiro
- Controller: ContaBancariaController
- Escopo desta documentacao: cadastrar, obter por id e editar conta bancaria

## 2. Contrato de consumo

### 2.1 POST /api/financeiro/contas-bancarias (Cadastrar)

- Metodo HTTP: POST
- Rota: /api/financeiro/contas-bancarias
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado

#### Request

Headers:
- Authorization: Bearer <token>
- Content-Type: application/json

Body:

```json
{
  "descricao": "Conta principal",
  "banco": "Banco XPTO",
  "agencia": "0001",
  "numero": "12345-6",
  "saldoInicial": 1500.00,
  "dataAbertura": "2026-04-01"
}
```

#### Response de sucesso

- Status code: 201 Created
- Body: ContaBancariaDto

```json
{
  "id": 10,
  "descricao": "Conta principal",
  "banco": "Banco XPTO",
  "agencia": "0001",
  "numero": "12345-6",
  "saldoInicial": 1500.00,
  "saldoAtual": 1500.00,
  "dataAbertura": "2026-04-01",
  "status": "ativa",
  "extrato": [],
  "logs": [
    {
      "id": 1,
      "data": "2026-04-01",
      "acao": "Cadastro",
      "descricao": "Conta bancaria criada com status ativa."
    }
  ]
}
```

### 2.2 GET /api/financeiro/contas-bancarias/{id} (Obter por id)

- Metodo HTTP: GET
- Rota: /api/financeiro/contas-bancarias/{id}
- Autenticacao: Bearer JWT obrigatorio
- Permissoes: usuario autenticado e dono do recurso

#### Request

Path params:
- id (long)

Headers:
- Authorization: Bearer <token>

#### Response de sucesso

- Status code: 200 OK
- Body: ContaBancariaDto

```json
{
  "id": 10,
  "descricao": "Conta principal",
  "banco": "Banco XPTO",
  "agencia": "0001",
  "numero": "12345-6",
  "saldoInicial": 1500.00,
  "saldoAtual": 1325.50,
  "dataAbertura": "2026-04-01",
  "status": "ativa",
  "extrato": [
    {
      "id": 100,
      "data": "2026-04-02",
      "descricao": "Ajuste manual",
      "tipo": "credito",
      "valor": 100.00
    }
  ],
  "logs": [
    {
      "id": 1,
      "data": "2026-04-01",
      "acao": "Cadastro",
      "descricao": "Conta bancaria criada com status ativa."
    }
  ]
}
```

### 2.3 PUT /api/financeiro/contas-bancarias/{id} (Editar)

- Metodo HTTP: PUT
- Rota: /api/financeiro/contas-bancarias/{id}
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
  "descricao": "Conta salario",
  "banco": "Banco XPTO",
  "agencia": "0001",
  "numero": "12345-6",
  "dataAbertura": "2026-04-01"
}
```

#### Response de sucesso

- Status code: 200 OK
- Body: ContaBancariaDto (atualizada)

```json
{
  "id": 10,
  "descricao": "Conta salario",
  "banco": "Banco XPTO",
  "agencia": "0001",
  "numero": "12345-6",
  "saldoInicial": 1500.00,
  "saldoAtual": 1325.50,
  "dataAbertura": "2026-04-01",
  "status": "ativa",
  "extrato": [],
  "logs": [
    {
      "id": 2,
      "data": "2026-04-03",
      "acao": "Atualizacao",
      "descricao": "Conta bancaria atualizada."
    }
  ]
}
```

## 3. Regras aplicadas

### 3.1 Validacoes de entrada

Fatos confirmados:
- `descricao`, `banco`, `agencia`, `numero` sao obrigatorios em cadastro e edicao; se vazio/whitespace retorna `campo_obrigatorio`.
- No cadastro, `saldoInicial` deve ser maior que zero; senao retorna `saldo_inicial_invalido`.
- `id` deve ser `long` por constraint de rota `{id:long}`.

### 3.2 Regras de negocio

Fatos confirmados:
- Todas as operacoes dependem de usuario autenticado.
- Obter por id e editar filtram por `id` + `usuario autenticado` no repositorio; nao existe acesso a recurso de outro usuario.
- Se nao encontrar conta no escopo do usuario, retorna `conta_bancaria_nao_encontrada`.
- No cadastro a conta nasce com:
  - `Status = Ativa`
  - `SaldoAtual = SaldoInicial`

### 3.3 Efeitos colaterais

Fatos confirmados:
- Persistencia:
  - POST cria registro de conta.
  - PUT atualiza registro existente.
- Logs:
  - POST adiciona log de cadastro.
  - PUT adiciona log de atualizacao.
- Integracoes/eventos:
  - Nao ha publicacao de evento externo nestes endpoints.

## 4. Erros e cenarios de falha

| Status | Condicao | Mensagem/retorno |
|---|---|---|
| 400 | Campos obrigatorios ausentes (POST/PUT) | `code: "campo_obrigatorio"` |
| 400 | Saldo inicial <= 0 (POST) | `code: "saldo_inicial_invalido"` |
| 401 | Token ausente/invalido | ProblemDetails de nao autorizado |
| 404 | Conta nao encontrada para usuario autenticado (GET/PUT) | `code: "conta_bancaria_nao_encontrada"` |
| 500 | Erro nao tratado | `code: "erro_interno"` |

## 5. Exemplos de consumo

### 5.1 Exemplo valido (Cadastrar)

```bash
curl -X POST "https://api.exemplo.com/api/financeiro/contas-bancarias" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "descricao": "Conta principal",
    "banco": "Banco XPTO",
    "agencia": "0001",
    "numero": "12345-6",
    "saldoInicial": 1500.00,
    "dataAbertura": "2026-04-01"
  }'
```

### 5.2 Exemplo com erro esperado (Obter por id inexistente)

```bash
curl -X GET "https://api.exemplo.com/api/financeiro/contas-bancarias/999999" \
  -H "Authorization: Bearer <token>"
```

Resposta esperada (404):

```json
{
  "status": 404,
  "title": "Recurso nao encontrado",
  "detail": "Conta bancaria nao encontrada.",
  "code": "conta_bancaria_nao_encontrada"
}
```

## 6. Rastreabilidade no codigo

- Controller/rota: `Core.Api/Controllers/Financeiro/ContaBancariaController.cs`
- Service/use case: `Core.Application/Services/Financeiro/ContaBancariaService.cs`
- Validator/schema: validacoes inline no service
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/ContaBancariaRepository.cs`
- Excecoes/enums: `DomainException`, `NotFoundException`, `StatusContaBancaria`
- Tratamento de erro HTTP: `Core.Api/Middlewares/ErrorHandlingMiddleware.cs` e `Core.Api/Extensions/ErroMensagemExtensions.cs`
- Testes relacionados: nao identificados nesta analise

## 7. Pendencias

- Pendencia: nao foi localizado teste automatizado especifico para POST/GET por id/PUT de conta bancaria.
- Impacto: risco de regressao em validacoes e codigos de erro.
- Acao sugerida: criar testes de integracao cobrindo cenarios de sucesso e falha (400/401/404).

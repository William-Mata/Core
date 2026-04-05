# Template de documentacao tecnica de regras da API

## 1. Contexto

- Modulo:
- Endpoint:
- Objetivo funcional:

## 2. Contrato de consumo

- Metodo HTTP:
- Rota:
- Autenticacao:
- Permissoes:

### 2.1 Request

- Query params:
- Path params:
- Headers:
- Body (JSON):

```json
{}
```

### 2.2 Response de sucesso

- Status code:
- Body (JSON):

```json
{}
```

## 3. Regras aplicadas

### 3.1 Validacoes de entrada

- Regra:
- Quando dispara:
- Resultado:

### 3.2 Regras de negocio

- Regra:
- Condicao:
- Acao:

### 3.3 Efeitos colaterais

- Persistencia:
- Logs:
- Integracoes/eventos:

## 4. Erros e cenarios de falha

| Status | Condicao | Mensagem/retorno |
|---|---|---|
| 400 |  |  |
| 401 |  |  |
| 403 |  |  |
| 404 |  |  |
| 409 |  |  |
| 500 |  |  |

## 5. Exemplos de consumo

### 5.1 Exemplo valido

```bash
curl -X POST "https://api.exemplo.com/recurso" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{}'
```

### 5.2 Exemplo com erro esperado

```bash
curl -X POST "https://api.exemplo.com/recurso" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{}'
```

## 6. Rastreabilidade no codigo

- Controller/rota:
- Service/use case:
- Validator/schema:
- Repository:
- Excecoes/enums:
- Testes relacionados:

## 7. Pendencias (se houver)

- Pendencia:
- Impacto:
- Acao sugerida:

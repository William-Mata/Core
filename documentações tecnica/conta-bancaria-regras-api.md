# Conta Bancaria - Regras de API

## 1. Resumo
- Modulo: Financeiro
- Controller: `Core.Api/Controllers/Financeiro/ContaBancariaController.cs`
- Rota base: `/api/financeiro/contas-bancarias`
- Endpoints atuais:
- `GET /api/financeiro/contas-bancarias`
- `GET /api/financeiro/contas-bancarias/{id}`
- `GET /api/financeiro/contas-bancarias/{id}/lancamentos`
- `POST /api/financeiro/contas-bancarias`
- `PUT /api/financeiro/contas-bancarias/{id}`
- `POST /api/financeiro/contas-bancarias/{id}/inativar`
- `POST /api/financeiro/contas-bancarias/{id}/ativar`

## 2. Contratos
- `CriarContaBancariaRequest`: `descricao`, `banco`, `agencia`, `numero`, `saldoInicial`, `dataAbertura`.
- `AtualizarContaBancariaRequest`: `descricao`, `banco`, `agencia`, `numero`, `dataAbertura`.
- `AlternarStatusContaBancariaRequest`: `quantidadePendencias` (default 0).
- `ContaBancariaDto`: dados da conta + extrato + logs.
- `GET /{id}/lancamentos` retorna `IReadOnlyCollection<LancamentoVinculadoDto>`.

## 3. Regras de negocio
- Todas as rotas exigem JWT.
- Usuario autenticado obrigatorio (`usuario_nao_autenticado`).
- `GET /{id}`, `PUT /{id}` e operacoes de status retornam `conta_bancaria_nao_encontrada` para recurso fora do escopo.
- Criacao:
- campos textuais obrigatorios (`campo_obrigatorio`).
- `saldoInicial` nao pode ser negativo (`saldo_inicial_invalido`).
- inicia `SaldoAtual = SaldoInicial` e `Status = Ativa`.
- Atualizacao:
- valida campos obrigatorios.
- preserva saldo e registra log de atualizacao.
- Inativacao:
- exige status `Ativa` e `quantidadePendencias = 0`.
- erro: `status_invalido` ou `conta_com_pendencias`.
- Ativacao:
- exige status `Inativa`.
- `GET /{id}/lancamentos` filtra historico por conta + competencia opcional.

## 4. Erros reais
- `400`: `campo_obrigatorio`, `saldo_inicial_invalido`, `status_invalido`, `conta_com_pendencias`, `usuario_nao_autenticado`, `dados_invalidos`.
- `401`: token ausente/invalido.
- `404`: `conta_bancaria_nao_encontrada`.
- `500`: `erro_interno`.

## 5. Exemplos
### 5.1 Listar
```bash
curl -X GET "https://api.exemplo.com/api/financeiro/contas-bancarias" -H "Authorization: Bearer <token>"
```

### 5.2 Ativar
```bash
curl -X POST "https://api.exemplo.com/api/financeiro/contas-bancarias/10/ativar" \
  -H "Authorization: Bearer <token>"
```

### 5.3 Lancamentos por competencia
```bash
curl -X GET "https://api.exemplo.com/api/financeiro/contas-bancarias/10/lancamentos?competencia=2026-04" \
  -H "Authorization: Bearer <token>"
```

## 6. Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/ContaBancariaController.cs`
- Service: `Core.Application/Services/Financeiro/ContaBancariaService.cs`
- DTOs: `Core.Application/DTOs/Financeiro/ContaBancariaDtos.cs`, `Core.Application/DTOs/Financeiro/FinanceiroListaDtos.cs`
- Validator: `Core.Application/Validators/Financeiro/CriarContaBancariaRequestValidator.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/ContaBancariaRepository.cs`
- Erros HTTP: `Core.Api/Middlewares/ErrorHandlingMiddleware.cs`

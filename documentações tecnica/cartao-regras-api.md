# Cartao - Regras de API

## 1. Resumo
- Modulo: Financeiro
- Controller: `Core.Api/Controllers/Financeiro/CartaoController.cs`
- Rota base: `/api/financeiro/cartoes`
- Endpoints atuais:
- `GET /api/financeiro/cartoes`
- `GET /api/financeiro/cartoes/{id}`
- `GET /api/financeiro/cartoes/{id}/lancamentos`
- `POST /api/financeiro/cartoes`
- `PUT /api/financeiro/cartoes/{id}`
- `POST /api/financeiro/cartoes/{id}/inativar`
- `POST /api/financeiro/cartoes/{id}/ativar`

## 2. Contratos
- `CriarCartaoRequest`: `descricao`, `bandeira`, `tipo`, `limite?`, `saldoDisponivel`, `diaVencimento?`, `dataVencimentoCartao?`.
- `AtualizarCartaoRequest`: `descricao`, `bandeira`, `tipo`, `limite?`, `diaVencimento?`, `dataVencimentoCartao?`.
- `AlternarStatusCartaoRequest`: `quantidadePendencias` (default 0).
- `CartaoDto`: cartao + status + logs.
- `GET /{id}/lancamentos` retorna `IReadOnlyCollection<LancamentoVinculadoDto>`.

## 3. Regras de negocio
- Todas as rotas exigem JWT.
- Usuario autenticado e obrigatorio (`usuario_nao_autenticado`).
- `GET /{id}` e `PUT /{id}` e operacoes de status retornam `cartao_nao_encontrado` fora do escopo do usuario.
- Validacoes centrais (`CartaoService.Validar`):
- `descricao` e `bandeira` obrigatorios (`campo_obrigatorio`).
- `tipo` deve ser enum valido (`tipo_invalido`).
- `saldoDisponivel >= 0` (`saldo_invalido`).
- para `Credito`, exige `limite > 0`, `diaVencimento` e `dataVencimentoCartao` (`dados_credito_obrigatorios`).
- Criacao inicia com `Status = Ativo` e log `Cadastro`.
- Atualizacao recalcula saldo disponivel quando limite de cartao de credito muda.
- Inativacao: exige status atual `Ativo` e `quantidadePendencias = 0`.
- Ativacao: exige status atual `Inativo`.
- `GET /{id}/lancamentos` filtra historico por cartao + competencia opcional.

## 4. Erros reais
- `400`: `campo_obrigatorio`, `tipo_invalido`, `saldo_invalido`, `dados_credito_obrigatorios`, `status_invalido`, `cartao_com_pendencias`, `usuario_nao_autenticado`, `dados_invalidos`.
- `401`: token ausente/invalido.
- `404`: `cartao_nao_encontrado`.
- `500`: `erro_interno`.

## 5. Exemplos
### 5.1 Listar
```bash
curl -X GET "https://api.exemplo.com/api/financeiro/cartoes" -H "Authorization: Bearer <token>"
```

### 5.2 Inativar
```bash
curl -X POST "https://api.exemplo.com/api/financeiro/cartoes/20/inativar" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"quantidadePendencias":0}'
```

### 5.3 Listar lancamentos por competencia
```bash
curl -X GET "https://api.exemplo.com/api/financeiro/cartoes/20/lancamentos?competencia=2026-04" \
  -H "Authorization: Bearer <token>"
```

## 6. Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/CartaoController.cs`
- Service: `Core.Application/Services/Financeiro/CartaoService.cs`
- DTOs: `Core.Application/DTOs/Financeiro/CartaoDtos.cs`, `Core.Application/DTOs/Financeiro/FinanceiroListaDtos.cs`
- Validator: `Core.Application/Validators/Financeiro/CriarCartaoRequestValidator.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/CartaoRepository.cs`
- Erros HTTP: `Core.Api/Middlewares/ErrorHandlingMiddleware.cs`

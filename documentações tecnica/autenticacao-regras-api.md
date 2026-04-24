# Autenticacao - Regras de API

## 1. Resumo
- Modulo: Administracao
- Controller: `Core.Api/Controllers/AutenticacaoController.cs`
- Rota base: `/api/autenticacao`
- Endpoints atuais:
- `POST /api/autenticacao/entrar`
- `POST /api/autenticacao/criar-primeira-senha`
- `POST /api/autenticacao/renovar-token`
- `POST /api/autenticacao/esqueci-senha`
- Endpoints descontinuados: nenhum identificado no codigo atual.

## 2. Contratos e regras por endpoint

### 2.1 `POST /api/autenticacao/entrar`
- Request (`EntrarRequest`):
```json
{
  "email": "usuario@dominio.com",
  "senha": "SenhaForte123"
}
```
- Response 200 (`AutenticacaoSuccessResponse`):
```json
{
  "accessToken": "jwt",
  "refreshToken": "refresh-...",
  "expiracao": "2026-04-24T18:00:00Z",
  "usuario": {
    "id": 1,
    "nome": "Admin",
    "email": "usuario@dominio.com",
    "status": true,
    "perfil": { "id": 1, "nome": "Administrador" },
    "modulosAtivos": []
  }
}
```
- Validacoes:
- `EntrarRequestValidator`: email obrigatorio/valido, senha obrigatoria e minimo 10.
- `AutenticacaoService`: reforca email/senha obrigatorios e regex de email.
- Regras criticas:
- bloqueia login quando usuario esta em `PrimeiroAcesso` (`primeiro_acesso_requer_criacao_senha`).
- bloqueia apos 5 tentativas invalidas (`login_bloqueado`).
- sucesso zera contador e gera access token + refresh token (TTL refresh: 7 dias).
- Efeitos colaterais:
- incrementa/zera tentativas de login.
- persiste refresh token novo.

### 2.2 `POST /api/autenticacao/criar-primeira-senha`
- Request (`CriarPrimeiraSenhaRequest`):
```json
{
  "email": "usuario@dominio.com",
  "senha": "SenhaForte123",
  "confirmarSenha": "SenhaForte123"
}
```
- Response 200:
```json
{ "mensagem": "Senha criada com sucesso." }
```
- Validacoes:
- validator exige email valido, senha >= 10 e confirmacao igual.
- service exige usuario ativo e `PrimeiroAcesso = true`.
- Efeitos colaterais:
- atualiza hash de senha e encerra estado de primeiro acesso.

### 2.3 `POST /api/autenticacao/renovar-token`
- Request (`RenovarTokenRequest`):
```json
{ "refreshToken": "refresh-..." }
```
- Response 200: mesmo contrato do login (`AutenticacaoSuccessResponse`).
- Regras:
- refresh token obrigatorio e valido.
- revoga o refresh token usado e emite novo par de tokens.
- usuario do token precisa estar ativo.
- Efeitos colaterais:
- revoga refresh token anterior.
- persiste novo refresh token.

### 2.4 `POST /api/autenticacao/esqueci-senha`
- Request (`EsqueciSenhaRequest`):
```json
{ "email": "usuario@dominio.com" }
```
- Response 200:
```json
{ "mensagem": "Se o email estiver cadastrado, as instrucoes de recuperacao serao enviadas." }
```
- Regras:
- email obrigatorio e valido.
- nao expoe existencia de usuario.
- Efeitos colaterais:
- zera tentativas invalidas para o email informado.

## 3. Erros reais observados
- `400`: `email_obrigatorio`, `senha_obrigatoria`, `email_invalido`, `login_bloqueado`, `credenciais_invalidas`, `primeiro_acesso_requer_criacao_senha`, `senha_fraca`, `confirmacao_senha_diferente`, `primeira_senha_ja_definida`, `refresh_token_obrigatorio`, `refresh_token_invalido`, `usuario_inativo_ou_nao_encontrado`, `dados_invalidos`.
- `404`: nao aplicavel nos endpoints deste controller.
- `500`: `erro_interno`.

## 4. Rastreabilidade
- Controller: `Core.Api/Controllers/AutenticacaoController.cs`
- Service: `Core.Application/Services/Administracao/AutenticacaoService.cs`
- DTOs: `Core.Application/DTOs/Administracao/AutenticacaoDtos.cs`
- Validators: `Core.Application/Validators/Administracao/EntrarRequestValidator.cs`, `CriarPrimeiraSenhaRequestValidator.cs`
- Repositorios/infra: `IAutenticacaoRepository`, `ITentativaLoginRepository`, `ITokenService`
- Middleware de erro: `Core.Api/Middlewares/ErrorHandlingMiddleware.cs`

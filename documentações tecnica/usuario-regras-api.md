# Usuario - Regras de API

## 1. Resumo
- Modulo: Administracao
- Controller: `Core.Api/Controllers/UsuarioController.cs`
- Rota base: `/api/usuarios`
- Endpoints atuais:
- `POST /api/usuarios/alterar-senha`
- `GET /api/usuarios` (ADMIN)
- `GET /api/usuarios/{id}` (ADMIN)
- `POST /api/usuarios` (AllowAnonymous)
- `PUT /api/usuarios/{id}` (ADMIN)
- `DELETE /api/usuarios/{id}` (ADMIN)
- Endpoints descontinuados: nenhum identificado no codigo atual.

## 2. Autenticacao e autorizacao
- Controller possui `[Authorize]` por padrao.
- Excecao: `POST /api/usuarios` possui `[AllowAnonymous]`.
- Rotas administrativas exigem role `ADMIN`.

## 3. Contratos e regras por endpoint

### 3.1 `POST /api/usuarios/alterar-senha`
- Request (`AlterarSenhaRequest`):
```json
{
  "senhaAtual": "SenhaAtual123",
  "novaSenha": "SenhaNova123",
  "confirmarSenha": "SenhaNova123"
}
```
- Response 200:
```json
{ "mensagem": "Senha alterada com sucesso." }
```
- Validacoes: obrigatoriedade, minimo 10, confirmacao igual, nova senha diferente da atual.
- Regras: valida senha atual no repositorio antes de alterar.

### 3.2 `GET /api/usuarios`
- Query: `id`, `descricao`, `dataInicio`, `dataFim`.
- Response 200 (`ListarUsuariosResponse`):
```json
{
  "sucesso": true,
  "dados": [
    {
      "id": 1,
      "nome": "Admin",
      "email": "admin@dominio.com",
      "perfil": "ADMIN",
      "dataNascimento": "1990-01-01",
      "dataCriacao": "2026-01-01T10:00:00Z"
    }
  ],
  "quantidade": 1
}
```

### 3.3 `GET /api/usuarios/{id}`
- Response 200 (`ObterUsuarioResponse`) inclui modulos/telas/funcionalidades ativos/inativos do usuario.
- Regra: retorna `usuario_nao_encontrado` quando inexistente.

### 3.4 `POST /api/usuarios`
- Request (`SalvarUsuarioRequest`):
```json
{
  "nome": "Novo Usuario",
  "email": "novo@dominio.com",
  "perfil": "USER",
  "dataNascimento": "1995-05-10",
  "status": true,
  "modulosAtivos": []
}
```
- Response 200 (`CriarUsuarioResponse`).
- Regras criticas:
- cadastro anonimo so aceita perfil `USER`.
- impede email duplicado (`email_em_uso`).
- cria usuario em `PrimeiroAcesso = true`.
- quando criado por admin, pode sincronizar permissoes por modulo/tela/funcionalidade.

### 3.5 `PUT /api/usuarios/{id}`
- Request: `SalvarUsuarioRequest`.
- Response 200:
```json
{ "sucesso": true, "mensagem": "Usuario atualizado com sucesso" }
```
- Regras:
- usuario alvo precisa existir.
- email nao pode conflitar com outro usuario.
- perfil mapeado para `ADMIN`/`USER`.
- sincroniza permissoes quando `modulosAtivos` informado.

### 3.6 `DELETE /api/usuarios/{id}`
- Response 200:
```json
{ "sucesso": true, "mensagem": "Usuario removido com sucesso" }
```
- Regras:
- exclusao logica (`Ativo = false`).
- admin nao pode remover a si proprio (`usuario_admin_nao_pode_excluir_a_si_mesmo`).

## 4. Validacoes declarativas
- `SalvarUsuarioRequestValidator`:
- nome/email/perfil obrigatorios.
- email por regex.
- perfil restrito a `ADMIN` ou `USER`.
- `DataNascimento` entre `1900-01-01` e data atual (BR).
- estrutura de `modulosAtivos` com ids obrigatorios.
- `AlterarSenhaRequestValidator`: campos obrigatorios + minimo 10 + confirmacao.

## 5. Erros reais observados
- `400`: `usuario_nao_autenticado`, `nome_obrigatorio`, `email_obrigatorio`, `email_invalido`, `perfil_invalido`, `email_em_uso`, `senha_atual_obrigatoria`, `nova_senha_obrigatoria`, `senha_fraca`, `nova_senha_igual_senha_atual`, `confirmacao_senha_diferente`, `senha_atual_incorreta`, `usuario_inativo_ou_nao_encontrado`, `usuario_admin_nao_pode_excluir_a_si_mesmo`, `dados_invalidos`.
- `401`: falta/invalidade de JWT nas rotas protegidas.
- `403`: acesso sem role ADMIN nas rotas administrativas.
- `404`: `usuario_nao_encontrado`.
- `500`: `erro_interno`.

## 6. Rastreabilidade
- Controller: `Core.Api/Controllers/UsuarioController.cs`
- Service: `Core.Application/Services/Administracao/UsuarioService.cs`
- DTOs: `Core.Application/DTOs/Administracao/UsuarioDtos.cs`
- Validators: `Core.Application/Validators/Administracao/SalvarUsuarioRequestValidator.cs`, `AlterarSenhaRequestValidator.cs`
- Middleware de erro: `Core.Api/Middlewares/ErrorHandlingMiddleware.cs`

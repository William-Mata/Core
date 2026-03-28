# Backend de Autenticacao - Clean Architecture

## Objetivo
Implementar o backend de autenticacao conforme o contrato da tela de login em `documentação tecnica/tela-login.md`, preservando separacao de responsabilidades, baixo acoplamento e regras de negocio isoladas.

## Estrutura de camadas
- `Core.Domain`
- `Authentication/LoginPolicy.cs`: regra de limite de tentativas invalidas (`5`).
- `Validation/EmailValidator.cs`: validacao de formato de email alinhada ao front.

- `Core.Application`
- `UseCases/Entrar/EntrarUseCase.cs`: orquestra validacoes, bloqueio por tentativas, autenticacao e reset.
- `UseCases/EsqueciSenha/EsqueciSenhaUseCase.cs`: valida email e reinicia bloqueio local.
- `UseCases/Autenticacao/AutenticacaoErrorCatalog.cs`: catalogo central de codigos/mensagens de erro.
- `Contracts/*`: abstrações para autenticacao e armazenamento de tentativas.
- `Models/AutenticacaoSuccessResponse.cs`: contrato de sucesso com mapeamento JSON explicito.

- `Core.Infrastructure`
- `Authentication/InMemoryAutenticacaoRepository.cs`: implementacao mockada com credenciais `admin@core.com` / `123456`.
- `Authentication/InMemoryTentativasLoginStore.cs`: controle de tentativas por email em memoria.

- `Core.API`
- `Controllers/AutenticacaoController.cs`: endpoints HTTP e mapeamento de resposta por tipo de falha.
- `Contracts/*`: contratos de entrada/saida da API.

## Decisoes de arquitetura e boas praticas
- SRP: use cases encapsulam regra de negocio; controller apenas traduz HTTP.
- DIP: `EntrarUseCase` e `EsqueciSenhaUseCase` dependem de interfaces (`IAutenticacaoRepository`, `ITentativasLoginStore`).
- DRY: codigos e mensagens de erro centralizados em `AutenticacaoErrorCatalog`.
- Contrato estavel: propriedades JSON esperadas pelo front sao fixadas com `JsonPropertyName`.
- Previsibilidade: validacoes e retorno de status HTTP seguem regras deterministicas por tipo de falha.

## Contratos HTTP
### `POST /api/autenticacao/entrar`
- Entrada:
```json
{
  "email": "usuario@empresa.com",
  "senha": "123456"
}
```
- Sucesso (`200`): retorna `accessToken`, `refreshToken`, `expiracao` e `usuario` com `perfil` e `modulosAtivos`.
- Validacao (`400`): campos obrigatorios ou email invalido.
- Credenciais invalidas (`401`): retorna `tentativasRestantes`.
- Bloqueio (`423`): retorna `bloqueado = true`.

### `POST /api/autenticacao/esqueci-senha`
- Entrada:
```json
{
  "email": "usuario@empresa.com"
}
```
- Sucesso (`200`): mensagem simulada de recuperacao.
- Validacao (`400`): email obrigatorio ou invalido.
- Efeito colateral: zera tentativas invalidas e remove bloqueio local.

## Regras implementadas
- Regex de email: `^[^\s@]+@[^\s@]+\.[^\s@]+$`.
- Bloqueio ao atingir a 5a tentativa invalida.
- `esqueci-senha` remove bloqueio local e reinicia contador.
- Contrato de perfil e modulos ativo/inativo conforme expectativa do front.

## Testes
- Aplicacao:
- `Core.Tests/Application/EntrarUseCaseTests.cs`
- `Core.Tests/Application/EsqueciSenhaUseCaseTests.cs`

- Integracao:
- `Core.Tests/Integration/AutenticacaoControllerTests.cs`

Coberturas principais:
- Sucesso no login.
- Validacoes de email e senha obrigatoria.
- Incremento de tentativas invalidas.
- Bloqueio na quinta tentativa com retorno de erro esperado.
- Reset via `esqueci-senha`.
- Preservacao do contrato retornado para o front.

# Estrutura do Projeto Core

## Mapa rapido

- `Core.Api`: entrada HTTP, configuracao do host, autenticacao JWT, middlewares e controllers.
- `Core.Application`: DTOs, validators e servicos de aplicacao que orquestram os casos de uso.
- `Core.Domain`: entidades, enums, excecoes e interfaces sem dependencia de infraestrutura.
- `Core.Infrastructure`: EF Core, `AppDbContext`, repositorios, DI e componentes de seguranca.
- `src/tests/Core.Tests`: testes unitarios, integracao e placeholder E2E.
- `documentação tecnica`: contratos e descricoes funcionais das telas.
- `query's`: scripts SQL por dominio.

## Entradas principais

- `Core.Api/Program.cs`: registra controllers, CORS, FluentValidation, autenticacao JWT, autorizacao, infraestrutura, OpenAPI e `ErrorHandlingMiddleware`.
- `Core.Infrastructure/DependencyInjection.cs`: liga interfaces do dominio a repositorios e registra os servicos de aplicacao.
- `Core.Api/appsettings.json`: define `ConnectionStrings:DefaultConnection` e parametros JWT.

## Responsabilidade por camada

### API

Editar `Core.Api/Controllers` ao adicionar ou mudar rotas, verbos HTTP, atributos `[Authorize]` ou forma de resposta.

Editar `Core.Api/Middlewares/ErrorHandlingMiddleware.cs` apenas quando o contrato global de erro precisar mudar.

Manter controllers enxutos, como os ja existentes em autenticacao, dashboard e financeiro.

### Application

Editar `Core.Application/Services` para regras de fluxo e uso do dominio.

Editar `Core.Application/DTOs` quando o contrato de entrada ou saida da aplicacao mudar.

Editar `Core.Application/Validators` quando a validacao estiver modelada com FluentValidation.

Padrao atual:
- Autenticacao concentra validacoes e geracao de tokens em `Core.Application/Services/AutenticacaoService.cs`.
- Financeiro concentra regras de criacao, atualizacao, efetivacao, cancelamento e estorno em `Core.Application/Services/Financeiro`.

### Domain

Editar `Core.Domain/Entities` para modelo de negocio.

Editar `Core.Domain/Interfaces` para novos contratos de persistencia, autenticacao ou integracao.

Editar `Core.Domain/Enums` e `Core.Domain/Exceptions` quando a regra de negocio exigir novos estados ou tipos de falha.

Evitar dependencias de ASP.NET, EF Core ou configuracao aqui.

### Infrastructure

Editar `Core.Infrastructure/Persistence/AppDbContext.cs` ao incluir novas entidades, relacionamentos, tabelas, seeds ou conversoes.

Editar `Core.Infrastructure/Persistence/Repositories` para consultas e persistencia concreta.

Editar `Core.Infrastructure/Security` para hashing, JWT ou outros detalhes de seguranca.

## Modulos do sistema

### Autenticacao

- Controller: `Core.Api/Controllers/AutenticacaoController.cs`
- Service: `Core.Application/Services/AutenticacaoService.cs`
- Interfaces: `Core.Domain/Interfaces/IAutenticacaoRepository.cs`, `ITentativaLoginRepository.cs`, `ITokenService.cs`
- Infra: `Core.Infrastructure/Persistence/Repositories/AutenticacaoRepository.cs`, `TentativaLoginRepository.cs`, `Core.Infrastructure/Security/JwtTokenService.cs`
- Testes: `src/tests/Core.Tests/Unit/Application/AutenticacaoServiceTests.cs`

Preservar:
- validacao basica de email e senha
- bloqueio apos 5 tentativas invalidas
- fluxo de primeiro acesso
- refresh token com revogacao e geracao de novo token

### Dashboard

- Controller: `Core.Api/Controllers/DashboardController.cs`
- Service: `Core.Application/Services/DashboardService.cs`

Usar para metricas agregadas e leitura autenticada.

### Financeiro

Controllers:
- `Core.Api/Controllers/Financeiro/ContaBancariaController.cs`
- `Core.Api/Controllers/Financeiro/CartaoController.cs`
- `Core.Api/Controllers/Financeiro/DespesaController.cs`
- `Core.Api/Controllers/Financeiro/ReceitaController.cs`

Services:
- `Core.Application/Services/Financeiro/ContaBancariaService.cs`
- `Core.Application/Services/Financeiro/CartaoService.cs`
- `Core.Application/Services/Financeiro/DespesaService.cs`
- `Core.Application/Services/Financeiro/ReceitaService.cs`

Domain:
- `Core.Domain/Entities/Financeiro`
- `Core.Domain/Interfaces/Financeiro`
- `Core.Domain/Enums`

Infra:
- `Core.Infrastructure/Persistence/Repositories/Financeiro`

Testes:
- `src/tests/Core.Tests/Unit/Application/*ServiceTests.cs`

Preservar:
- status validos
- logs de auditoria
- calculo de valores liquidos
- uso do usuario autenticado via `IUsuarioAutenticadoProvider`

### Administracao de usuarios

- Controller: `Core.Api/Controllers/UsuarioController.cs`
- Service: `Core.Application/Services/UsuarioService.cs`
- Domain: entidades em `Core.Domain/Entities/Administracao`
- Infra: `Core.Infrastructure/Persistence/Repositories/UsuarioRepository.cs`

## Testes e armadilhas

Usar `src/tests/Core.Tests/Core.Tests.csproj` como projeto real de testes.

Tratar `tests/Core.Tests` como residuo de build ate que a estrutura seja consolidada; hoje esse caminho nao contem as classes de teste.

Comandos uteis:

```powershell
dotnet test src/tests/Core.Tests/Core.Tests.csproj
dotnet test src/tests/Core.Tests/Core.Tests.csproj --filter AutenticacaoServiceTests
dotnet test src/tests/Core.Tests/Core.Tests.csproj --filter DespesaServiceTests
```

## Documentacao e SQL

Ler `documentação tecnica/tela-*.md` quando a mudanca vier do front-end ou de uma tela especifica.

Ler `documentação tecnica/*clean-architecture.md` quando a tarefa pedir aderencia ao desenho funcional ja descrito.

Consultar `query's/00-script-mestre.sql` e as pastas tematicas para entender seeds, tabelas e consultas de apoio.

## Estrategia de implementacao

Para uma nova funcionalidade:

1. Comecar pelo contrato HTTP ja existente ou esperado na documentacao.
2. Ajustar DTOs e validators.
3. Implementar ou alterar o service.
4. Expandir interfaces do dominio so quando necessario.
5. Implementar persistencia na infraestrutura.
6. Atualizar `AppDbContext` se a modelagem mudar.
7. Cobrir o fluxo em `src/tests/Core.Tests`.

Para um bug:

1. Reproduzir pelo controller e service.
2. Confirmar se a falha e de validacao, regra de negocio, mapeamento ou persistencia.
3. Corrigir na camada responsavel, sem empurrar regra de negocio para controller ou repositorio.
4. Fixar o comportamento com teste.

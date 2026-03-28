---
name: core-clean-architecture-api
description: Orientar mudancas no backend Core em .NET 10 com Clean Architecture. Use quando Codex precisar implementar, corrigir, revisar ou testar funcionalidades deste projeto, especialmente em autenticacao, dashboard, administracao de usuarios e financeiro, navegando entre Core.Api, Core.Application, Core.Domain, Core.Infrastructure, src/tests/Core.Tests, documentacao tecnica e query's.
---

# Core Clean Architecture API

## Objetivo

Trabalhar no backend deste repositorio sem quebrar a separacao entre API, aplicacao, dominio, infraestrutura e testes.

Ler [references/project-structure.md](references/project-structure.md) antes de alterar partes grandes do fluxo ou quando houver duvida sobre onde cada mudanca deve entrar.

## Fluxo de trabalho

1. Identificar o modulo afetado: autenticacao, dashboard, administracao de usuarios ou financeiro.
2. Localizar a entrada HTTP em `Core.Api/Controllers`.
3. Seguir para o servico correspondente em `Core.Application/Services`.
4. Confirmar DTOs e validators em `Core.Application/DTOs` e `Core.Application/Validators`.
5. Preservar regras e contratos do dominio em `Core.Domain`.
6. Ajustar persistencia e integracoes em `Core.Infrastructure`.
7. Atualizar ou criar testes em `src/tests/Core.Tests`.
8. Validar com `dotnet test src/tests/Core.Tests/Core.Tests.csproj` ou com um filtro de teste mais especifico.

## Aplicar mudancas na camada certa

Manter controllers finos. Receber a requisicao HTTP, delegar ao servico e traduzir apenas detalhes de transporte.

Manter a orquestracao de caso de uso em `Core.Application`. Colocar ali validacoes de fluxo, montagem de DTOs, controle de status e chamadas a interfaces do dominio.

Manter o dominio livre de detalhes de ASP.NET, EF Core e configuracao. Colocar entidades, enums, excecoes e contratos em `Core.Domain`.

Manter implementacao de repositorios, `AppDbContext`, seguranca e DI em `Core.Infrastructure`.

Lancar `DomainException` e `NotFoundException` para erros de negocio e ausencia de registro, deixando `Core.Api/Middlewares/ErrorHandlingMiddleware.cs` cuidar do contrato HTTP.

## Regras praticas deste projeto

Seguir o padrao existente de nomeacao: `*Controller`, `*Service`, `*Repository`, `*Request`, `*Dto`, `*Validator`.

Ao alterar autenticacao, preservar a cadeia `AutenticacaoController` -> `AutenticacaoService` -> interfaces de `Core.Domain.Interfaces` -> implementacoes em `Core.Infrastructure`, incluindo JWT, refresh token e bloqueio por tentativas invalidas.

Ao alterar o modulo financeiro, preservar a logica de status, logs e usuario autenticado dentro dos servicos de `Core.Application/Services/Financeiro`.

Ao alterar persistencia, espelhar mudancas no `Core.Infrastructure/Persistence/AppDbContext.cs` e nos repositorios afetados.

Usar `documentação tecnica/` e `query's/` como fonte de contrato funcional e de apoio de banco quando a solicitacao vier do front ou da modelagem SQL.

Desconfiar de caminhos duplicados. O codigo-fonte de testes esta em `src/tests/Core.Tests`, enquanto `tests/Core.Tests` hoje contem apenas artefatos residuais.

## Validar

Preferir testes de unidade ou integracao do modulo alterado antes de rodar a suite inteira.

Usar comandos como `dotnet test src/tests/Core.Tests/Core.Tests.csproj --filter AutenticacaoServiceTests` para validacao rapida.

Rodar a suite completa quando tocar contratos compartilhados, `AppDbContext`, middlewares, autenticacao ou servicos financeiros reutilizados por varios endpoints.

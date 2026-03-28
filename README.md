# Core

Backend .NET organizado em camadas, com separacao entre API, aplicacao, dominio, infraestrutura e testes.

## Estrutura

- `Core.Api`: entrada HTTP, autenticacao, middlewares e composicao da aplicacao
- `Core.Application`: casos de uso, servicos, DTOs e validacoes
- `Core.Domain`: entidades, contratos e excecoes de dominio
- `Core.Infrastructure`: persistencia, repositorios e integracoes tecnicas
- `Core.Tests`: testes unitarios e de integracao
- `documentacao tecnica`: documentacao funcional e tecnica de apoio
- `query's`: scripts SQL de referencia para ambiente local

## Solucao

Arquivos principais:

- `Core.sln`
- `Core.slnx`

Projeto de testes:

- `Core.Tests/Core.Tests.csproj`

## Requisitos

- .NET SDK 10
- SQL Server disponivel no ambiente de desenvolvimento

## Configuracao local

As configuracoes de ambiente da API devem ser fornecidas localmente por arquivos de configuracao nao sensiveis, variaveis de ambiente ou secrets do ambiente de desenvolvimento.

Nao versionar credenciais, chaves, tokens, connection strings reais ou dados internos da aplicacao no repositorio.

## Banco de dados

Script principal de apoio:

- `query's/00-script-mestre.sql`

Use esse material apenas como referencia para criacao ou sincronizacao do ambiente local.

## Executar a API

```powershell
dotnet run --project .\Core.Api\Core.Api.csproj
```

## Testes

```powershell
dotnet test .\Core.Tests\Core.Tests.csproj
```

## Observacoes

- O `.gitignore` ja cobre artefatos comuns de build, teste e tooling local.
- Antes de subir mudancas maiores, valide pelo menos build e testes do projeto afetado.

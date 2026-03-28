# Core

API .NET para o domínio Financeiro, organizada em camadas e com solução separada por responsabilidade.

## Estrutura

- `Core.Api`: camada HTTP, autenticação, middlewares e configuração da aplicação
- `Core.Application`: serviços, DTOs e regras de orquestração
- `Core.Domain`: entidades, interfaces e exceções de domínio
- `Core.Infrastructure`: persistência, repositórios e infraestrutura técnica
- `src/tests/Core.Tests`: testes unitários e de integração
- `query's`: scripts SQL do banco `Financeiro`

## Solução

Arquivos principais:

- `Core.sln`
- `Core.slnx`

O projeto de testes referenciado pela solução está em:

- `src/tests/Core.Tests/Core.Tests.csproj`

## Requisitos

- .NET SDK 10
- SQL Server local

## Configuração local

A API usa a connection string definida em `Core.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=Financeiro;Trusted_Connection=True;TrustServerCertificate=True"
}
```

JWT de desenvolvimento também é configurado no mesmo arquivo.

## Banco de dados

Script principal:

- `query's/00-script-mestre.sql`

Esse script cria a base `Financeiro`, tabelas, constraints, índices e seeds principais.

## Executar a API

```powershell
dotnet run --project .\Core.Api\Core.Api.csproj
```

A API roda em ambiente local e expõe endpoints como:

- `https://localhost:5001/api/autenticacao/*`
- `https://localhost:5001/api/usuarios/*`
- `https://localhost:5001/api/financeiro/*`

## Testes

Executar a suíte:

```powershell
dotnet test .\src\tests\Core.Tests\Core.Tests.csproj
```

## Observações

- O repositório usa `.gitignore` ajustado para artefatos locais de build, teste e tooling.
- A estrutura de testes válida fica em `src/tests`; a pasta `tests` legada foi removida da solução.

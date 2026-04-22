# Core

API backend em **.NET 10** organizada em camadas, seguindo uma abordagem inspirada em **Clean Architecture**, com separaÃ§Ã£o entre **API**, **Application**, **Domain**, **Infrastructure** e **Tests**.

O projeto concentra regras de **autenticaÃ§Ã£o**, **administraÃ§Ã£o de usuÃ¡rios** e mÃ³dulos do domÃ­nio **financeiro**, com suporte a **SQL Server**, **JWT**, **FluentValidation**, **Entity Framework Core** e processamento assÃ­ncrono com **RabbitMQ**.

Tambem inclui o modulo **Compras** com listas compartilhadas, desejos de compra, historico de precos e rastreabilidade via logs.

## VisÃ£o geral

O repositÃ³rio estÃ¡ estruturado para manter responsabilidades bem separadas:

- **Core.Api**: camada de entrada HTTP, autenticaÃ§Ã£o, controllers, middlewares e composiÃ§Ã£o da aplicaÃ§Ã£o.
- **Core.Application**: serviÃ§os, contratos, DTOs e validaÃ§Ãµes.
- **Core.Domain**: entidades, enums, exceÃ§Ãµes e interfaces de domÃ­nio.
- **Core.Infrastructure**: persistÃªncia, seguranÃ§a, mensageria, storage e injeÃ§Ã£o de dependÃªncia.
- **Core.Tests**: testes unitÃ¡rios e de integraÃ§Ã£o.

## Funcionalidades observadas no projeto

Pelos controllers, serviÃ§os e documentaÃ§Ã£o tÃ©cnica versionada, o projeto cobre principalmente:

- autenticaÃ§Ã£o de usuÃ¡rios
- administraÃ§Ã£o de usuÃ¡rios
- contas bancÃ¡rias
- cartÃµes
- despesas
- receitas
- reembolsos
- aprovaÃ§Ã£o de despesas e receitas
- relacionamento com amigo financeiro
- Ã¡reas e subÃ¡reas do mÃ³dulo financeiro
- listas de compras compartilhadas
- desejos de compra
- historico de produtos/precos para apoio de compra

## Stack utilizada

- **.NET 10**
- **ASP.NET Core Web API**
- **Entity Framework Core 10**
- **SQL Server**
- **JWT Bearer Authentication**
- **FluentValidation**
- **RabbitMQ**
- **xUnit** para testes

## Estrutura do projeto

```text
Core
â”œâ”€â”€ Core.Api
â”‚   â”œâ”€â”€ Controllers
â”‚   â”‚   â”œâ”€â”€ Administracao
â”‚   â”‚   â””â”€â”€ Financeiro
â”‚   â”œâ”€â”€ Extensions
â”‚   â”œâ”€â”€ Middlewares
â”‚   â”œâ”€â”€ Security
â”‚   â”œâ”€â”€ Program.cs
â”‚   â””â”€â”€ appsettings.json
â”œâ”€â”€ Core.Application
â”‚   â”œâ”€â”€ Contracts
â”‚   â”œâ”€â”€ DTOs
â”‚   â”œâ”€â”€ Services
â”‚   â””â”€â”€ Validators
â”œâ”€â”€ Core.Domain
â”‚   â”œâ”€â”€ Entities
â”‚   â”œâ”€â”€ Enums
â”‚   â”œâ”€â”€ Exceptions
â”‚   â””â”€â”€ Interfaces
â”œâ”€â”€ Core.Infrastructure
â”‚   â”œâ”€â”€ Messaging
â”‚   â”œâ”€â”€ Persistence
â”‚   â”œâ”€â”€ Security
â”‚   â”œâ”€â”€ Storage
â”‚   â””â”€â”€ DependencyInjection.cs
â”œâ”€â”€ Core.Tests
â”‚   â”œâ”€â”€ Integration
â”‚   â””â”€â”€ Unit
â”œâ”€â”€ documentaÃ§Ãµes tecnica
â”œâ”€â”€ query's
â”œâ”€â”€ skills
â”œâ”€â”€ .codex
â”œâ”€â”€ Core.sln
â””â”€â”€ Core.slnx
```

## Principais componentes tÃ©cnicos

### API
A API registra controllers, autenticaÃ§Ã£o JWT, autorizaÃ§Ã£o, validaÃ§Ã£o automÃ¡tica com FluentValidation, tratamento global de erros via middleware e OpenAPI em ambiente de desenvolvimento.

### AutenticaÃ§Ã£o e seguranÃ§a
A aplicaÃ§Ã£o utiliza **JWT** com configuraÃ§Ãµes de `Issuer`, `Audience` e `Secret` vindas da configuraÃ§Ã£o da aplicaÃ§Ã£o.

### PersistÃªncia
A infraestrutura registra um `AppDbContext` com **SQL Server** via `ConnectionStrings:DefaultConnection`.

### Processamento assÃ­ncrono
Existe configuraÃ§Ã£o de **RabbitMQ** e serviÃ§os para publicaÃ§Ã£o e consumo em background relacionados a recorrÃªncia financeira.

### Testes
O projeto possui suÃ­te de testes separada em **Unit** e **Integration**, usando **xUnit** e `Microsoft.NET.Test.Sdk`.

## PrÃ©-requisitos

Antes de rodar o projeto localmente, tenha instalado:

- **.NET SDK 10**
- **SQL Server** acessÃ­vel no ambiente local
- **RabbitMQ** local ou remoto configurado
- Uma IDE como **Visual Studio 2022+** ou **VS Code** com extensÃ£o C#

## ConfiguraÃ§Ã£o local

Atualmente a API depende de configuraÃ§Ãµes para:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Secret`
- `RabbitMq:*`

### RecomendaÃ§Ã£o

Para evitar expor dados sensÃ­veis no repositÃ³rio:

1. mantenha no repositÃ³rio apenas um arquivo de exemplo, como `appsettings.Example.json`
2. use `appsettings.Development.json` localmente
3. prefira variÃ¡veis de ambiente ou User Secrets para segredos
4. nunca versione secrets reais, tokens, senhas ou connection strings de ambientes reais

### Exemplo de configuraÃ§Ã£o local

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CoreDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "Core.Api",
    "Audience": "Core.Frontend",
    "Secret": "SUA_CHAVE_COM_NO_MINIMO_32_BYTES_AQUI"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

## Como executar o projeto

### Restaurar dependÃªncias

```bash
dotnet restore .\Core.sln
```

### Compilar a soluÃ§Ã£o

```bash
dotnet build .\Core.sln
```

### Executar a API

```bash
dotnet run --project .\Core.Api\Core.Api.csproj
```

## Testes

Para executar os testes:

```bash
dotnet test .\Core.Tests\Core.Tests.csproj
```

## DocumentaÃ§Ã£o e scripts auxiliares

O repositÃ³rio tambÃ©m possui materiais de apoio:

- **documentaÃ§Ãµes tecnica/**: documentaÃ§Ã£o funcional e tÃ©cnica das telas e mÃ³dulos
- **query's/**: scripts SQL de apoio e script mestre para ambiente local
- **skills/** e **.codex/**: arquivos auxiliares para workflows com IA e documentaÃ§Ã£o tÃ©cnica complementar
- Documentacao de Compras:
  - documentações tecnica/lista-compra-regras-api.md
  - documentações tecnica/desejo-compra-regras-api.md
  - documentações tecnica/historico-produto-regras-api.md
- SQL de Compras:
  - query's/04-compras/25-compras.sql
  - SignalR: /hubs/compras (evento `listaAtualizada`)

## Boas prÃ¡ticas recomendadas para este repositÃ³rio

- manter regras de negÃ³cio concentradas em `Core.Application` e `Core.Domain`
- evitar lÃ³gica de negÃ³cio em controllers
- isolar integraÃ§Ãµes externas em `Core.Infrastructure`
- nÃ£o versionar segredos nem arquivos locais de ambiente
- manter documentaÃ§Ã£o de mÃ³dulo prÃ³xima ao domÃ­nio correspondente
- usar um arquivo de ignore especÃ­fico para IA, como `.cursorignore`, para reduzir contexto desnecessÃ¡rio e economizar tokens

## PrÃ³ximos passos sugeridos

- criar um `appsettings.Example.json`
- mover segredos locais para `appsettings.Development.json` ou User Secrets
- documentar endpoints principais por mÃ³dulo
- incluir exemplos de requests/responses no README ou em documentaÃ§Ã£o separada

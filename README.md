# Core

API backend em **.NET 10** organizada em camadas, seguindo uma abordagem inspirada em **Clean Architecture**, com separação entre **API**, **Application**, **Domain**, **Infrastructure** e **Tests**.

O projeto concentra regras de **autenticação**, **administração de usuários** e módulos do domínio **financeiro**, com suporte a **SQL Server**, **JWT**, **FluentValidation**, **Entity Framework Core** e processamento assíncrono com **RabbitMQ**.

## Visão geral

O repositório está estruturado para manter responsabilidades bem separadas:

- **Core.Api**: camada de entrada HTTP, autenticação, controllers, middlewares e composição da aplicação.
- **Core.Application**: serviços, contratos, DTOs e validações.
- **Core.Domain**: entidades, enums, exceções e interfaces de domínio.
- **Core.Infrastructure**: persistência, segurança, mensageria, storage e injeção de dependência.
- **Core.Tests**: testes unitários e de integração.

## Funcionalidades observadas no projeto

Pelos controllers, serviços e documentação técnica versionada, o projeto cobre principalmente:

- autenticação de usuários
- administração de usuários
- contas bancárias
- cartões
- despesas
- receitas
- reembolsos
- aprovação de despesas e receitas
- relacionamento com amigo financeiro
- áreas e subáreas do módulo financeiro

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
├── Core.Api
│   ├── Controllers
│   │   ├── Administracao
│   │   └── Financeiro
│   ├── Extensions
│   ├── Middlewares
│   ├── Security
│   ├── Program.cs
│   └── appsettings.json
├── Core.Application
│   ├── Contracts
│   ├── DTOs
│   ├── Services
│   └── Validators
├── Core.Domain
│   ├── Entities
│   ├── Enums
│   ├── Exceptions
│   └── Interfaces
├── Core.Infrastructure
│   ├── Messaging
│   ├── Persistence
│   ├── Security
│   ├── Storage
│   └── DependencyInjection.cs
├── Core.Tests
│   ├── Integration
│   └── Unit
├── documentações tecnica
├── query's
├── skills
├── .codex
├── Core.sln
└── Core.slnx
```

## Principais componentes técnicos

### API
A API registra controllers, autenticação JWT, autorização, validação automática com FluentValidation, tratamento global de erros via middleware e OpenAPI em ambiente de desenvolvimento.

### Autenticação e segurança
A aplicação utiliza **JWT** com configurações de `Issuer`, `Audience` e `Secret` vindas da configuração da aplicação.

### Persistência
A infraestrutura registra um `AppDbContext` com **SQL Server** via `ConnectionStrings:DefaultConnection`.

### Processamento assíncrono
Existe configuração de **RabbitMQ** e serviços para publicação e consumo em background relacionados a recorrência financeira.

### Testes
O projeto possui suíte de testes separada em **Unit** e **Integration**, usando **xUnit** e `Microsoft.NET.Test.Sdk`.

## Pré-requisitos

Antes de rodar o projeto localmente, tenha instalado:

- **.NET SDK 10**
- **SQL Server** acessível no ambiente local
- **RabbitMQ** local ou remoto configurado
- Uma IDE como **Visual Studio 2022+** ou **VS Code** com extensão C#

## Configuração local

Atualmente a API depende de configurações para:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Secret`
- `RabbitMq:*`

### Recomendação

Para evitar expor dados sensíveis no repositório:

1. mantenha no repositório apenas um arquivo de exemplo, como `appsettings.Example.json`
2. use `appsettings.Development.json` localmente
3. prefira variáveis de ambiente ou User Secrets para segredos
4. nunca versione secrets reais, tokens, senhas ou connection strings de ambientes reais

### Exemplo de configuração local

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

### Restaurar dependências

```bash
dotnet restore .\Core.sln
```

### Compilar a solução

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

## Documentação e scripts auxiliares

O repositório também possui materiais de apoio:

- **documentações tecnica/**: documentação funcional e técnica das telas e módulos
- **query's/**: scripts SQL de apoio e script mestre para ambiente local
- **skills/** e **.codex/**: arquivos auxiliares para workflows com IA e documentação técnica complementar

## Boas práticas recomendadas para este repositório

- manter regras de negócio concentradas em `Core.Application` e `Core.Domain`
- evitar lógica de negócio em controllers
- isolar integrações externas em `Core.Infrastructure`
- não versionar segredos nem arquivos locais de ambiente
- manter documentação de módulo próxima ao domínio correspondente
- usar um arquivo de ignore específico para IA, como `.cursorignore`, para reduzir contexto desnecessário e economizar tokens

## Próximos passos sugeridos

- criar um `appsettings.Example.json`
- mover segredos locais para `appsettings.Development.json` ou User Secrets
- documentar endpoints principais por módulo
- incluir exemplos de requests/responses no README ou em documentação separada

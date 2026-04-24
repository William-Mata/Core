# Core Backend

Backend em .NET 10 organizado em camadas (API, Application, Domain, Infrastructure e Tests), com foco em autenticação, administração de usuários, financeiro e compras colaborativas.

## Visão geral
- `Core.Api`: controllers HTTP, middlewares, configuração de JWT, SignalR e bootstrap da aplicação.
- `Core.Application`: serviços, DTOs, contratos e validações.
- `Core.Domain`: entidades, enums, exceções e interfaces de domínio.
- `Core.Infrastructure`: EF Core, repositórios, segurança, mensageria e DI.
- `Core.Tests`: testes unitários e de integração.

## Módulos principais
- Administração: autenticação (login, refresh token, primeira senha e esqueci senha) e gestão de usuários/permissões.
- Financeiro: contas, cartões, despesas, receitas, reembolsos, faturas, histórico e amizade/rateio.
- Compras: listas compartilhadas, itens, desejos, histórico de preço, logs e atualização em tempo real.

## Stack
- .NET 10 / ASP.NET Core Web API
- Entity Framework Core 10
- SQL Server
- JWT Bearer
- FluentValidation
- SignalR
- RabbitMQ (infra disponível)
- xUnit

## Arquitetura e padrões do projeto

### 1) Estilo arquitetural: camadas com inspiração em Clean Architecture
- As dependências fluem para dentro:
- `Core.Api` depende de `Core.Application`.
- `Core.Application` depende de `Core.Domain`.
- `Core.Infrastructure` implementa interfaces de `Core.Domain` e é conectada na composição via DI.
- `Core.Domain` não depende de ASP.NET, EF Core ou detalhes de transporte.

### 2) Responsabilidade por camada
- API (transport layer):
- controllers finos, sem regra de negócio central.
- mapeamento de entrada HTTP para DTOs.
- autenticação/autorização, CORS, Swagger e pipeline.
- Application (use cases):
- orquestração de regras de negócio.
- validações de fluxo, estados, recorrência, aprovação e rateio.
- Domain:
- entidades, enums, exceções (`DomainException`, `NotFoundException`) e contratos.
- Infrastructure:
- repositórios, `AppDbContext`, token JWT, storage e mensageria.

### 3) Padrões aplicados
- Repository pattern: acesso a dados via interfaces no domínio e implementações na infraestrutura.
- Dependency Injection: composição central em `Core.Infrastructure/DependencyInjection.cs` + `Program.cs`.
- DTO pattern: requests/responses desacoplados das entidades persistidas.
- Validation pipeline:
- validação declarativa com FluentValidation (`AddFluentValidationAutoValidation`).
- validações de negócio complementares em services.
- Exception to HTTP mapping:
- middleware global converte exceções em `ProblemDetails`.
- `DomainException` -> 400.
- `NotFoundException` -> 404.
- exceção não tratada -> 500.
- Real-time pub/sub:
- atualização de Compras via SignalR (`/hubs/compras`) com evento `listaAtualizada`.
- Background messaging:
- publisher/consumer RabbitMQ para processamento assíncrono relacionado ao domínio financeiro.

### 4) Convenções de nomenclatura e organização
- `*Controller`, `*Service`, `*Repository`, `*Request`, `*Dto`, `*Validator`.
- Controllers em `Core.Api/Controllers/<Modulo>`.
- Serviços em `Core.Application/Services/<Modulo>`.
- DTOs em `Core.Application/DTOs/<Modulo>`.
- Repositórios em `Core.Infrastructure/Persistence/Repositories/<Modulo>`.

### 5) Fluxo padrão de requisição
1. Requisição entra no controller.
2. DTO é validado (FluentValidation + model binding).
3. Controller delega para service de Application.
4. Service aplica regras de negócio e chama interfaces de repositório.
5. Infrastructure executa persistência/integração.
6. Resultado volta como DTO.
7. Em caso de erro, o middleware global padroniza `ProblemDetails`.

### 6) Segurança aplicada
- JWT Bearer com validação de issuer, audience, signing key e expiração.
- `ClockSkew = TimeSpan.Zero`.
- autorização por `[Authorize]` e `[Authorize(Roles = "ADMIN")]` quando necessário.
- `UsuarioAutenticadoProvider` resolve `usuarioId` por claims (`NameIdentifier`, `sub`, `usuario_id`).

## Estrutura de pastas
```text
Core
├── Core.Api
│   ├── Controllers
│   ├── Hubs
│   ├── Middlewares
│   ├── Security
│   └── Program.cs
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
│   ├── Persistence
│   ├── Security
│   ├── Messaging
│   └── DependencyInjection.cs
├── Core.Tests
└── documentações tecnica
```

## Configuração
A aplicação espera estas chaves:
- `ConnectionStrings:DefaultConnection`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Secret`
- `RabbitMq:*`

Exemplo:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CoreDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Issuer": "Core.Api",
    "Audience": "Core.Frontend",
    "Secret": "SUA_CHAVE_COM_NO_MINIMO_32_BYTES"
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

## Como executar
```bash
dotnet restore .\Core.sln
dotnet build .\Core.sln
dotnet run --project .\Core.Api\Core.Api.csproj
```

## Testes
```bash
dotnet test .\Core.Tests\Core.Tests.csproj
```

## Documentação técnica
Documentos de regras e contratos da API ficam em `documentações tecnica/`, incluindo:
- `backend-auditoria-regras-api.md`
- `autenticacao-regras-api.md`
- `usuario-regras-api.md`
- `despesa-regras-api.md`, `receita-regras-api.md`, `reembolso-regras-api.md`
- `lista-compra-regras-api.md`, `desejo-compra-regras-api.md`, `historico-produto-regras-api.md`

## Segurança
- Não versionar segredos reais, tokens ou connection strings de ambientes reais.
- Manter segredos em variáveis de ambiente, User Secrets ou arquivo local não versionado.

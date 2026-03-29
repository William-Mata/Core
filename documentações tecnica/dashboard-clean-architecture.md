# Backend da Dashboard - Clean Architecture

## Objetivo
Implementar o endpoint da dashboard conforme `documentação tecnica/tela-dashboard.md`, preservando o contrato aderente ao front atual: dados de `transacoes` e `balanco` para calculos locais da UI.

## Endpoint
- `GET /api/dashboard`

Resposta:
- `transacoes`: lista de transacoes efetivadas.
- `balanco`: lista de contas/cartoes com saldo pronto para exibicao.

## Arquitetura aplicada
- `Core.Domain`
- Regras compartilhadas permanecem no dominio (sem acoplamento com infraestrutura).

- `Core.Application`
- `Contracts/IDashboardRepository.cs`: contrato de obtencao dos dados.
- `Models/DashboardResponse.cs`: modelo de saida do caso de uso com mapeamento de nomes JSON esperados pelo front.
- `UseCases/Dashboard/ObterDashboardUseCase.cs`: orquestracao do fluxo de leitura.

- `Core.Infrastructure`
- `Dashboard/InMemoryDashboardRepository.cs`: implementacao em memoria com dados mockados aderentes ao contrato.

- `Core.API`
- `Controllers/DashboardController.cs`: exposicao HTTP e mapeamento para contratos de API.
- `Contracts/DashboardResponse.cs`: contratos de resposta da API.

## Boas praticas aplicadas
- DIP: use case depende da abstracao `IDashboardRepository`.
- SRP: controller apenas traduz HTTP e mapeia resposta.
- Contrato estavel: campos seguem exatamente os nomes esperados pelo front (`dataEfetivacao`, `codigoPagamento`, `tipoPagamento`, etc.).
- Dados tipados: valores monetarios como `decimal` e data como `DateOnly`, evitando formatacao de string no backend.

## Testes criados
- Aplicacao:
- `Core.Tests/Application/ObterDashboardUseCaseTests.cs`

- Integracao:
- `Core.Tests/Integration/DashboardControllerTests.cs`

Cobertura principal:
- Retorno de dados pelo caso de uso.
- Contrato HTTP da dashboard com campos obrigatorios em `transacoes` e `balanco`.

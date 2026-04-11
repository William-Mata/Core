# AreaSubAreaFinanceiroController - Regras de API

## Rotas
- `GET /api/financeiro/areas-subareas`
- `GET /api/financeiro/areas-subareas/soma-rateio`

## Regras principais
- Exige autenticacao JWT.
- Query `tipo` e opcional (`Despesa` ou `Receita`, case-insensitive).
- Quando `tipo` e invalido, retorna `tipo_area_invalido`.
- `GET /areas-subareas` retorna estrutura de areas e subareas cadastradas por tipo.
- `GET /areas-subareas/soma-rateio` retorna areas/subareas com `ValorTotalRateio`.
- A soma de rateio considera apenas lancamentos efetivados:
  - `Despesa.Status == Efetivada`
  - `Receita.Status == Efetivada`
- O calculo de soma de rateio e restrito ao usuario autenticado.

## Contrato de saida (soma-rateio)
- `AreaRateioListaDto`
  - `id`
  - `nome`
  - `tipo`
  - `valorTotalRateio`
  - `subAreas[]`
- `SubAreaRateioListaDto`
  - `id`
  - `nome`
  - `valorTotalRateio`

## Rastreabilidade
- Controller: `Core.Api/Controllers/Financeiro/AreaSubAreaFinanceiroController.cs`
- Service: `Core.Application/Services/Financeiro/AreaSubAreaFinanceiroService.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Financeiro/AreaRepository.cs`

# Auditoria Tecnica do Backend - API

## 1. Escopo auditado
- Data da auditoria: 2026-04-24
- Fonte de verdade: codigo em `Core.Api`, `Core.Application`, `Core.Domain`, `Core.Infrastructure`.
- Modulos obrigatorios auditados: Administracao, Financeiro, Compras.
- Restricao aplicada: nenhuma alteracao de codigo; apenas documentacao tecnica.

## 2. Inventario de controllers e endpoints

### 2.1 Administracao
- `AutenticacaoController` (`/api/autenticacao`)
- `POST /api/autenticacao/entrar`
- `POST /api/autenticacao/criar-primeira-senha`
- `POST /api/autenticacao/renovar-token`
- `POST /api/autenticacao/esqueci-senha`
- `UsuarioController` (`/api/usuarios`)
- `POST /api/usuarios/alterar-senha`
- `GET /api/usuarios` (ADMIN)
- `GET /api/usuarios/{id}` (ADMIN)
- `POST /api/usuarios` (anonimo permitido)
- `PUT /api/usuarios/{id}` (ADMIN)
- `DELETE /api/usuarios/{id}` (ADMIN)

### 2.2 Financeiro
- `AmigoController` (`/api/financeiro/amigos`)
- `GET /api/financeiro/amigos`
- `POST /api/financeiro/amigos/convites`
- `GET /api/financeiro/amigos/convites`
- `POST /api/financeiro/amigos/convites/{id}/aceitar`
- `POST /api/financeiro/amigos/convites/{id}/rejeitar`
- `DELETE /api/financeiro/amigos/{amigoId}`
- `AreaSubAreaFinanceiroController` (`/api/financeiro/areas-subareas`)
- `GET /api/financeiro/areas-subareas`
- `GET /api/financeiro/areas-subareas/soma-rateio`
- `CartaoController` (`/api/financeiro/cartoes`)
- `GET /api/financeiro/cartoes`
- `GET /api/financeiro/cartoes/{id}`
- `GET /api/financeiro/cartoes/{id}/lancamentos`
- `POST /api/financeiro/cartoes`
- `PUT /api/financeiro/cartoes/{id}`
- `POST /api/financeiro/cartoes/{id}/inativar`
- `POST /api/financeiro/cartoes/{id}/ativar`
- `ContaBancariaController` (`/api/financeiro/contas-bancarias`)
- `GET /api/financeiro/contas-bancarias`
- `GET /api/financeiro/contas-bancarias/{id}`
- `GET /api/financeiro/contas-bancarias/{id}/lancamentos`
- `POST /api/financeiro/contas-bancarias`
- `PUT /api/financeiro/contas-bancarias/{id}`
- `POST /api/financeiro/contas-bancarias/{id}/inativar`
- `POST /api/financeiro/contas-bancarias/{id}/ativar`
- `DespesaController` (`/api/financeiro/despesas`)
- `GET /api/financeiro/despesas`
- `GET /api/financeiro/despesas/{id}`
- `POST /api/financeiro/despesas`
- `PUT /api/financeiro/despesas/{id}`
- `POST /api/financeiro/despesas/{id}/efetivar`
- `POST /api/financeiro/despesas/{id}/cancelar`
- `POST /api/financeiro/despesas/{id}/estornar`
- `GET /api/financeiro/despesas/pendentes-aprovacao`
- `POST /api/financeiro/despesas/{id}/aprovar`
- `POST /api/financeiro/despesas/{id}/rejeitar`
- `ReceitaController` (`/api/financeiro/receitas`)
- `GET /api/financeiro/receitas`
- `GET /api/financeiro/receitas/{id}`
- `POST /api/financeiro/receitas`
- `PUT /api/financeiro/receitas/{id}`
- `POST /api/financeiro/receitas/{id}/efetivar`
- `POST /api/financeiro/receitas/{id}/cancelar`
- `POST /api/financeiro/receitas/{id}/estornar`
- `GET /api/financeiro/receitas/pendentes-aprovacao`
- `POST /api/financeiro/receitas/{id}/aprovar`
- `POST /api/financeiro/receitas/{id}/rejeitar`
- `ReembolsoController` (`/api/financeiro/reembolsos`)
- `GET /api/financeiro/reembolsos`
- `GET /api/financeiro/reembolsos/{id}`
- `POST /api/financeiro/reembolsos`
- `PUT /api/financeiro/reembolsos/{id}`
- `DELETE /api/financeiro/reembolsos/{id}`
- `POST /api/financeiro/reembolsos/{id}/efetivar`
- `POST /api/financeiro/reembolsos/{id}/estornar`
- `FaturaCartaoController` (`/api/financeiro/faturas-cartao`)
- `GET /api/financeiro/faturas-cartao`
- `GET /api/financeiro/faturas-cartao/detalhes`
- `POST /api/financeiro/faturas-cartao/{id}/efetivar`
- `POST /api/financeiro/faturas-cartao/{id}/estornar`
- `HistoricoTransacaoFinanceiraController` (`/api/financeiro/historico-transacoes`)
- `GET /api/financeiro/historico-transacoes`
- `GET /api/financeiro/historico-transacoes/resumo`
- `GET /api/financeiro/historico-transacoes/resumo-por-ano`

### 2.3 Compras
- `ListaCompraController` (`/api/compras/listas`)
- `GET /api/compras/listas`
- `GET /api/compras/listas/{id}`
- `GET /api/compras/listas/{id}/detalhe`
- `POST /api/compras/listas`
- `PUT /api/compras/listas/{id}`
- `POST /api/compras/listas/{id}/arquivar`
- `POST /api/compras/listas/{id}/duplicar`
- `DELETE /api/compras/listas/{id}`
- `GET /api/compras/listas/{id}/sugestoes-itens`
- `POST /api/compras/listas/{id}/itens`
- `PUT /api/compras/listas/{id}/itens/{itemId}`
- `PATCH /api/compras/listas/{id}/itens/{itemId}/edicao-rapida`
- `POST /api/compras/listas/{id}/itens/{itemId}/marcar-comprado`
- `POST /api/compras/listas/{id}/acoes-lote`
- `GET /api/compras/listas/{id}/logs`
- `DesejoCompraController` (`/api/compras/desejos`)
- `GET /api/compras/desejos`
- `POST /api/compras/desejos`
- `PUT /api/compras/desejos/{id}`
- `DELETE /api/compras/desejos/{id}`
- `POST /api/compras/desejos/converter`
- `HistoricoPrecoCompraController` (`/api/compras/historico-precos`)
- `GET /api/compras/historico-precos`

## 3. Regras criticas de servico (rastreaveis)
- Recorrencia (Despesa/Receita): validacoes de recorrencia fixa, quantidade de recorrencia e avancos de data (`Recorrencia`), incluindo escopo de atualizacao/cancelamento (`EscopoRecorrencia`).
- Fatura de cartao: reembolsos/despesas/receitas vinculadas a cartao chamam `FaturaCartaoService` para resolver fatura, bloquear alteracoes e recalcular totais.
- Compras em lote: `TipoAcaoLoteListaCompra` com 13 acoes, incluindo criacao de listas derivadas e mesclagem de duplicados.
- Participantes de lista: exige exatamente 1 proprietario, sem duplicidade, e somente amigos aceitos podem participar.

## 4. Integracoes

### 4.1 JWT e autorizacao
- Configurado em `Program.cs` com `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Secret`.
- Validacoes ativas: issuer, audience, assinatura, expiracao e `ClockSkew = 0`.
- Regras de role:
- `UsuarioController` exige `ADMIN` em listagem/obter/atualizar/excluir.
- Claims aceitas para id de usuario: `NameIdentifier`, `sub`, `usuario_id`.

### 4.2 SignalR
- Hub: `ComprasHub` em `/hubs/compras` (autenticado).
- Metodos do hub:
- `EntrarLista(listaId)` adiciona conexao ao grupo `compras_lista_{listaId}` quando usuario tem acesso.
- `SairLista(listaId)` remove conexao do grupo.
- Evento publicado: `listaAtualizada`.
- Payload real (`ListaCompraAtualizadaMessage`):
```json
{
  "listaId": 12,
  "evento": "item_atualizado",
  "usuarioId": 1,
  "dataHoraUtc": "2026-04-24T18:00:00Z"
}
```
- Eventos emitidos pelo service de Compras:
- `lista_criada`, `lista_atualizada`, `lista_arquivada`, `lista_excluida`, `lista_duplicada`
- `item_criado`, `item_atualizado`, `item_edicao_rapida`, `item_comprado`, `item_desmarcado`
- `lote_executado`, `desejos_convertidos`, `lista_derivada_criada`

## 5. Divergencias encontradas e tratadas
- Ausencia de documentacao de API para `AutenticacaoController`.
- Ausencia de documentacao de API para `UsuarioController`.
- `cartao-regras-api.md` nao cobria `GET listagem`, `GET lancamentos`, `POST ativar`, `POST inativar`.
- `conta-bancaria-regras-api.md` nao cobria `GET listagem`, `GET lancamentos`, `POST ativar`, `POST inativar`.
- `lista-compra-regras-api.md` ja marcava corretamente endpoints de participantes como descontinuados; confirmado no codigo atual.

## 6. Erros padrao mapeados
- `DomainException` -> 400.
- `NotFoundException` -> 404.
- `ValidationException`/ModelState -> 400 (`dados_invalidos`, com `errors`).
- Excecao nao tratada -> 500 (`erro_interno`).

## 7. Rastreabilidade principal
- Controllers: `Core.Api/Controllers/**`
- Services: `Core.Application/Services/**`
- DTOs: `Core.Application/DTOs/**`
- Validators: `Core.Application/Validators/**`
- Erros HTTP: `Core.Api/Middlewares/ErrorHandlingMiddleware.cs`, `Core.Api/Extensions/ErroMensagemExtensions.cs`
- SignalR: `Core.Api/Hubs/ComprasHub.cs`, `Core.Api/Hubs/ComprasTempoRealPublisher.cs`, `Core.Application/Contracts/Compras/ComprasTempoRealMessages.cs`

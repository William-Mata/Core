# ComprasHub / SignalR - Regras Tecnicas

## 1. Resumo
- Modulo:
  - tempo real de Compras via SignalR.
- Hub:
  - `ComprasHub` mapeado em `/hubs/compras`.
- Objetivo:
  - permitir que clientes autenticados entrem em grupos por lista de compra e recebam evento de atualizacao em tempo real.
- Metodos atuais do Hub:
  - `EntrarLista(long listaId, CancellationToken cancellationToken = default)`
  - `SairLista(long listaId, CancellationToken cancellationToken = default)`
- Metodos/eventos descontinuados:
  - nenhum identificado no codigo atual.

## 2. Contratos completos

### 2.1 Conexao ao Hub
- Transporte:
  - SignalR (`AddSignalR()`).
- Rota:
  - `/hubs/compras`.
- Autenticacao:
  - JWT obrigatorio (`[Authorize]` no Hub).
  - no fluxo SignalR, o backend aceita token em query string `access_token` para requests da rota `/hubs/compras`.
- CORS relevante:
  - politica permite `AllowCredentials()`.
  - origem permitida apenas quando `uri.IsLoopback` (localhost/127.0.0.1/::1).

### 2.2 Metodo `EntrarLista`
- Direcao:
  - cliente -> servidor (Hub invoke).
- Nome do metodo:
  - `EntrarLista`
- Parametros:
  - `listaId` (`long`)
- Regra principal:
  - usuario autenticado so entra no grupo se tiver acesso de visualizacao a lista.
- Efeito:
  - adiciona `ConnectionId` no grupo `compras_lista_{listaId}`.
- Erro funcional:
  - `HubException("lista_compra_sem_permissao_visualizacao")`.

Exemplo de invocacao (cliente):
```ts
await connection.invoke("EntrarLista", 12);
```

### 2.3 Metodo `SairLista`
- Direcao:
  - cliente -> servidor (Hub invoke).
- Nome do metodo:
  - `SairLista`
- Parametros:
  - `listaId` (`long`)
- Efeito:
  - remove `ConnectionId` do grupo `compras_lista_{listaId}`.

Exemplo de invocacao (cliente):
```ts
await connection.invoke("SairLista", 12);
```

### 2.4 Evento de servidor para clientes
- Nome do evento SignalR:
  - `listaAtualizada`
- Destino:
  - todos os clientes no grupo `compras_lista_{listaId}`.
- Payload completo (`ListaCompraAtualizadaMessage`):
```json
{
  "listaId": 12,
  "evento": "item_atualizado",
  "usuarioId": 1,
  "dataHoraUtc": "2026-04-25T15:00:00Z"
}
```
- Contrato do payload:
  - `listaId` (`long`)
  - `evento` (`string`)
  - `usuarioId` (`int`)
  - `dataHoraUtc` (`DateTime` UTC)

## 3. Regras de negocio e fluxo

### 3.1 Identidade do usuario no Hub
- Ordem de leitura de claim para `usuarioId`:
  - `ClaimTypes.NameIdentifier`
  - `sub`
  - `usuario_id`
- Se nao encontrar claim valida inteira:
  - `HubException("usuario_nao_autenticado")`.

### 3.2 Fluxo de inscricao em lista
1. Cliente abre conexao com `/hubs/compras` enviando JWT.
2. Cliente invoca `EntrarLista(listaId)`.
3. Hub valida usuario via claims.
4. Hub consulta repositorio para confirmar acesso a lista (`ObterListaAcessivelPorIdAsync`).
5. Se autorizado, adiciona conexao ao grupo da lista.
6. Quando o modulo Compras publica atualizacao, o evento `listaAtualizada` e enviado ao grupo.

### 3.3 Fluxo de publicacao de atualizacoes
1. Casos de uso de Compras executam alteracoes na lista/itens.
2. `ComprasService` chama `IComprasTempoRealPublisher.PublicarAtualizacaoListaAsync`.
3. `ComprasTempoRealPublisher` monta `ListaCompraAtualizadaMessage`.
4. Publisher envia `SendAsync("listaAtualizada", mensagem)` para o grupo da lista.

## 4. Eventos de negocio publicados atualmente
- `lista_criada`
- `lista_atualizada`
- `lista_arquivada`
- `lista_excluida`
- `lista_duplicada`
- `item_criado`
- `item_atualizado`
- `item_edicao_rapida`
- `item_comprado`
- `item_desmarcado`
- `item_excluido`
- `lote_executado`
- `desejos_convertidos`
- `lista_derivada_criada`

## 5. Erros e cenarios de falha

| Camada | Condicao | Retorno atual |
|---|---|---|
| Hub | usuario sem claim valida de id | `HubException("usuario_nao_autenticado")` |
| Hub | usuario sem acesso de visualizacao a lista | `HubException("lista_compra_sem_permissao_visualizacao")` |
| Conexao HTTP/SignalR | token ausente ou invalido no acesso ao Hub | falha de autenticacao/negociacao (na pratica, sem conexao autenticada) |
| Publisher | cancelamento do `CancellationToken` | publicacao cancelada com log informativo |
| Publisher | excecao ao enviar evento | log de warning; excecao nao e propagada para quebrar fluxo principal |

## 6. Efeitos colaterais
- Persistencia:
  - nenhuma no Hub/publisher de tempo real.
- Integracao:
  - envio de notificacao em tempo real para clientes conectados no grupo da lista.
- Logs:
  - `ComprasTempoRealPublisher` registra `Information` em cancelamento e `Warning` em falha de envio.

## 7. Rastreabilidade no codigo
- Hub:
  - `Core.Api/Hubs/ComprasHub.cs`
- Publicador de tempo real:
  - `Core.Api/Hubs/ComprasTempoRealPublisher.cs`
- Mapeamento e configuracao SignalR/JWT:
  - `Core.Api/Program.cs`
- Contratos de mensagem/grupo:
  - `Core.Application/Contracts/Compras/ComprasTempoRealMessages.cs`
- Contrato de publicacao:
  - `Core.Application/Contracts/Compras/IComprasTempoRealPublisher.cs`
- Origem dos disparos de evento:
  - `Core.Application/Services/Compras/ComprasService.cs`
- Repositorio usado na autorizacao de acesso a lista no Hub:
  - `Core.Domain/Interfaces/Compras/IComprasRepository.cs`
- Testes relacionados:
  - `Core.Tests/Unit/Application/ComprasServiceTests.cs` (usa fake de publisher; nao cobre handshake/metodos do Hub)

## 8. Fatos confirmados e inferencias

### 8.1 Fatos confirmados
- O Hub de Compras exige autenticacao (`[Authorize]`).
- A rota do Hub e `/hubs/compras`.
- O backend aceita `access_token` na query string para autenticacao do SignalR nessa rota.
- O evento enviado aos clientes e `listaAtualizada`.
- O nome do grupo por lista e `compras_lista_{listaId}`.
- Os eventos de negocio listados na secao 4 sao disparados pelo `ComprasService`.

### 8.2 Inferencias
- Em clientes com reconexao automatica, e necessario reinvocar `EntrarLista` para reassociar a nova conexao aos grupos da lista.
  - Motivo: associacao de grupo ocorre por `ConnectionId`.

## 9. Pendencias
- Nao foram identificados testes automatizados dedicados para `ComprasHub` (metodos `EntrarLista`/`SairLista`) e para o pipeline de autenticacao SignalR.

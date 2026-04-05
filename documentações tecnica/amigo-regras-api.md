# Documentacao tecnica de regras da API - AmigoController

## 1. Contexto

- Modulo: Financeiro
- Controller: `Core.Api/Controllers/Financeiro/AmigoController.cs`
- Rota base: `api/financeiro/amigos`
- Objetivo funcional: gerenciar amizade financeira entre usuarios autenticados (listar amigos, enviar/listar/responder convites e remover amizade).

## 2. Premissas de integracao

- Todas as rotas exigem autenticacao JWT (`[Authorize]` no controller).
- O usuario autenticado e resolvido por claims `NameIdentifier`, `sub` ou `usuario_id` (`UsuarioAutenticadoProvider`).
- Sem token valido, a pipeline de autenticacao bloqueia a requisicao antes do controller.
- O controller nao possui envelope proprio; retorna diretamente DTO/colecao via `Ok(...)` ou `NoContent()`.

## 3. Modelos de dados usados neste controller

### 3.1 Request - EnviarConviteAmizadeRequest

Fonte: `Core.Application/DTOs/Financeiro/AmizadeDtos.cs`

- `Email` (string): email do usuario destino do convite.
- `Mensagem` (string?, opcional): mensagem do convite, persistida em `ConviteAmizade` e retornada no `ConviteAmizadeDto`.

Exemplo:

```json
{
  "email": "destino@dominio.com",
  "mensagem": "Vamos compartilhar os lancamentos?"
}
```

### 3.2 Response - AmigoListaDto

Fonte: `Core.Application/DTOs/Financeiro/FinanceiroListaDtos.cs`

- `Id` (int): id do usuario amigo.
- `Nome` (string): nome do amigo.
- `Email` (string): email do amigo.

Exemplo de item:

```json
{
  "id": 12,
  "nome": "Alex Silva",
  "email": "alex@email.com"
}
```

### 3.3 Response - ConviteAmizadeDto

Fonte: `Core.Application/DTOs/Financeiro/AmizadeDtos.cs`

- `Id` (long): id do convite.
- `UsuarioOrigemNome` (string): nome do usuario origem.
- `Status` (string): valor textual em minusculo (`pendente`, `aceito`, `rejeitado`).
- `Mensagem` (string?, opcional): mensagem enviada junto ao convite.
- `DataHoraCadastro` (DateTime): data/hora de criacao do convite.

Exemplo:

```json
{
  "id": 101,
  "usuarioOrigemNome": "Maria",
  "status": "pendente",
  "mensagem": "Vamos compartilhar os lancamentos?",
  "dataHoraCadastro": "2026-04-04T18:23:11.522Z"
}
```

## 4. Endpoints

### 4.1 Listar amigos

- Finalidade: retornar amigos com amizade aceita do usuario autenticado.
- Metodo HTTP: `GET`
- Rota: `api/financeiro/amigos`
- Parametros de rota: nao possui
- Query params: nao possui
- Body: nao possui

Regras e validacoes:

- Exige usuario autenticado; sem usuario id valido em claim, o service lanca `DomainException("usuario_nao_autenticado")`.
- Retorna apenas amigos aceitos e ativos (filtro aplicado no repositorio em `ListarAmigosAceitosAsync`).
- Ordenacao por nome (ascendente) no repositorio.

Retorno esperado:

- `200 OK`
- Body: array de `AmigoListaDto` (pode vir vazio).

Estrutura principal da resposta:

```json
[
  {
    "id": 12,
    "nome": "Alex Silva",
    "email": "alex@email.com"
  }
]
```

Observacoes de integracao:

- Array vazio representa "sem amigos aceitos", nao erro.

### 4.2 Enviar convite de amizade

- Finalidade: criar convite pendente para outro usuario.
- Metodo HTTP: `POST`
- Rota: `api/financeiro/amigos/convites`
- Parametros de rota: nao possui
- Query params: nao possui
- Body esperado: `EnviarConviteAmizadeRequest`

Descricao dos campos do request:

- `email`: obrigatorio na pratica de negocio (service rejeita nulo/vazio e auto-convite).
- `mensagem`: opcional; atualmente nao interfere no processamento do convite.

Regras e validacoes:

- Usuario autenticado obrigatorio.
- `email` nao pode ser vazio.
- `email` nao pode ser igual ao email do proprio usuario logado.
- Usuario destino deve existir e estar ativo.
- Nao pode existir amizade ja aceita entre origem e destino.
- Nao pode existir convite pendente origem -> destino.
- Nao pode existir convite pendente destino -> origem.
- Convite criado com status inicial `pendente`.

Retorno esperado:

- `200 OK`
- Body: `ConviteAmizadeDto` do convite criado.
- Campo `direcao` no retorno desta rota: sempre `enviado`.
- Campo `status`: `pendente` no momento da criacao.

Estrutura principal da resposta:

```json
{
  "id": 101,
  "usuarioOrigemId": 5,
  "usuarioOrigemNome": "Maria",
  "usuarioDestinoId": 9,
  "usuarioDestinoNome": "Joao",
  "status": "pendente",
  "direcao": "enviado",
  "dataHoraCadastro": "2026-04-04T18:23:11.522Z",
  "dataHoraResposta": null
}
```

Observacoes de integracao:

- Mesmo enviando `mensagem`, ela nao aparece no retorno e nao altera regras.

### 4.3 Listar convites pendentes

- Finalidade: listar convites pendentes relacionados ao usuario autenticado.
- Metodo HTTP: `GET`
- Rota: `api/financeiro/amigos/convites`
- Parametros de rota: nao possui
- Query params: nao possui
- Body: nao possui

Regras e validacoes:

- Usuario autenticado obrigatorio.
- Lista apenas convites com status `pendente`.
- Inclui convites onde o usuario e origem ou destino.
- Ordenacao por data de cadastro decrescente.
- Campo `direcao` e calculado por convite:
- `enviado` quando `UsuarioOrigemId == usuario autenticado`
- `recebido` quando `UsuarioDestinoId == usuario autenticado`

Retorno esperado:

- `200 OK`
- Body: array de `ConviteAmizadeDto` (pode vir vazio).

Estrutura principal da resposta:

```json
[
  {
    "id": 101,
    "usuarioOrigemId": 5,
    "usuarioOrigemNome": "Maria",
    "usuarioDestinoId": 9,
    "usuarioDestinoNome": "Joao",
    "status": "pendente",
    "direcao": "recebido",
    "dataHoraCadastro": "2026-04-04T18:23:11.522Z",
    "dataHoraResposta": null
  }
]
```

Observacoes de integracao:

- A API nao retorna convites com status `aceito`/`rejeitado` nesta listagem.

### 4.4 Aceitar convite

- Finalidade: aceitar convite recebido pelo usuario autenticado.
- Metodo HTTP: `POST`
- Rota: `api/financeiro/amigos/convites/{id}/aceitar`
- Parametros de rota:
- `id` (long): id do convite
- Query params: nao possui
- Body: nao possui

Regras e validacoes:

- Usuario autenticado obrigatorio.
- Convite deve existir (`id` informado).
- Apenas o usuario destino do convite pode responder.
- Convite precisa estar em status `pendente`.
- Ao aceitar:
- atualiza convite para status `aceito`
- define `DataHoraResposta = DateTime.UtcNow`
- cria registro de amizade se ainda nao existir entre os usuarios

Retorno esperado:

- `200 OK`
- Body: `ConviteAmizadeDto` atualizado.
- Campo `direcao` no retorno desta rota: `recebido`.
- Campo `status`: `aceito`.

Estrutura principal da resposta:

```json
{
  "id": 101,
  "usuarioOrigemId": 5,
  "usuarioOrigemNome": "Maria",
  "usuarioDestinoId": 9,
  "usuarioDestinoNome": "Joao",
  "status": "aceito",
  "direcao": "recebido",
  "dataHoraCadastro": "2026-04-04T18:23:11.522Z",
  "dataHoraResposta": "2026-04-04T18:30:45.000Z"
}
```

Observacoes de integracao:

- Se o convite ja nao estiver pendente, a API rejeita com erro de dominio.

### 4.5 Rejeitar convite

- Finalidade: rejeitar convite recebido pelo usuario autenticado.
- Metodo HTTP: `POST`
- Rota: `api/financeiro/amigos/convites/{id}/rejeitar`
- Parametros de rota:
- `id` (long): id do convite
- Query params: nao possui
- Body: nao possui

Regras e validacoes:

- Regras identicas ao endpoint de aceite para permissao e status.
- Ao rejeitar:
- atualiza convite para status `rejeitado`
- define `DataHoraResposta = DateTime.UtcNow`
- nao cria amizade

Retorno esperado:

- `200 OK`
- Body: `ConviteAmizadeDto` atualizado.
- Campo `direcao` no retorno desta rota: `recebido`.
- Campo `status`: `rejeitado`.

Estrutura principal da resposta:

```json
{
  "id": 101,
  "usuarioOrigemId": 5,
  "usuarioOrigemNome": "Maria",
  "usuarioDestinoId": 9,
  "usuarioDestinoNome": "Joao",
  "status": "rejeitado",
  "direcao": "recebido",
  "dataHoraCadastro": "2026-04-04T18:23:11.522Z",
  "dataHoraResposta": "2026-04-04T18:31:02.000Z"
}
```

Observacoes de integracao:

- Convites rejeitados deixam de aparecer no `GET /api/financeiro/amigos/convites` (que lista apenas pendentes).

### 4.6 Remover amizade

- Finalidade: remover amizade existente entre usuario autenticado e amigo informado.
- Metodo HTTP: `DELETE`
- Rota: `api/financeiro/amigos/{amigoId}`
- Parametros de rota:
- `amigoId` (int): id do amigo
- Query params: nao possui
- Body: nao possui

Regras e validacoes:

- Usuario autenticado obrigatorio.
- `amigoId` deve ser maior que zero.
- `amigoId` nao pode ser igual ao id do proprio usuario autenticado.
- Deve existir amizade entre usuario autenticado e `amigoId`.

Retorno esperado:

- `204 No Content`
- Sem body.

Observacoes de integracao:

- Falha de validacao/nao encontrado retorna erro; sucesso nao retorna payload.

## 5. Contrato de erro HTTP

### 5.1 Formato de erro padrao da API (ProblemDetails)

Quando a requisicao cai no `ErrorHandlingMiddleware` ou na validacao automatica de ModelState, o retorno segue `application/problem+json`, com campos:

- `status` (int)
- `title` (string)
- `detail` (string)
- `type` (string)
- `instance` (string)
- `code` (extensao; codigo interno da aplicacao)
- `traceId` (extensao)
- `errors` (extensao opcional; lista de mensagens de validacao)

Exemplo:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Requisicao invalida",
  "status": 400,
  "detail": "Os dados informados sao invalidos.",
  "instance": "/api/financeiro/amigos/convites",
  "code": "dados_invalidos",
  "traceId": "00-...-..."
}
```

### 5.2 Erros de dominio/not found observados nos endpoints de amigo

Mapeamento de status:

- `DomainException` => `400`
- `NotFoundException` => `404`
- Excecao nao tratada => `500`

Codigos disparados pelo `AmigoService`:

- `usuario_nao_autenticado`
- `usuario_destino_invalido`
- `usuario_nao_encontrado`
- `amizade_ja_existente`
- `convite_ja_enviado`
- `convite_pendente_existente`
- `convite_nao_permitido`
- `status_convite_invalido`
- `amizade_nao_encontrada` (400 em validacao inicial do `amigoId`; 404 quando busca no repositorio nao encontra)
- `convite_nao_encontrado` (404)

Observacao importante:

- O dicionario `ErroMensagemExtensions` nao possui mensagens dedicadas para varios codigos especificos de amizade (`amizade_ja_existente`, `convite_ja_enviado`, `convite_nao_permitido` etc.).
- Nesses casos, o `detail` retorna a mensagem padrao: `Nao foi possivel processar a solicitacao.`
- O codigo real do erro continua disponivel em `code` e deve ser usado pelo front para tratamento.

### 5.3 401 Unauthorized

- Ocorre quando token JWT esta ausente/invalido/expirado.
- E produzido pelo middleware de autenticacao/autorizacao (antes da execucao do controller/service).
- O shape de payload para 401 nao e padronizado neste controller via `ErrorHandlingMiddleware`.

## 6. Exemplos de consumo

### 6.1 Listar amigos

```bash
curl -X GET "https://<host>/api/financeiro/amigos" \
  -H "Authorization: Bearer <token>"
```

### 6.2 Enviar convite

```bash
curl -X POST "https://<host>/api/financeiro/amigos/convites" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "destino@dominio.com",
    "mensagem": "Vamos compartilhar os lancamentos?"
  }'
```

### 6.3 Aceitar convite

```bash
curl -X POST "https://<host>/api/financeiro/amigos/convites/101/aceitar" \
  -H "Authorization: Bearer <token>"
```

### 6.4 Rejeitar convite

```bash
curl -X POST "https://<host>/api/financeiro/amigos/convites/101/rejeitar" \
  -H "Authorization: Bearer <token>"
```

### 6.5 Remover amizade

```bash
curl -X DELETE "https://<host>/api/financeiro/amigos/12" \
  -H "Authorization: Bearer <token>"
```

## 7. Rastreabilidade no codigo

- Controller:
- `Core.Api/Controllers/Financeiro/AmigoController.cs`
- Service:
- `Core.Application/Services/Financeiro/AmigoFinanceiroService.cs`
- DTOs:
- `Core.Application/DTOs/Financeiro/AmizadeDtos.cs`
- `Core.Application/DTOs/Financeiro/FinanceiroListaDtos.cs`
- Repositorio:
- `Core.Infrastructure/Persistence/Repositories/Financeiro/AmizadeRepository.cs`
- Entidades/enums:
- `Core.Domain/Entities/Financeiro/ConviteAmizade.cs`
- `Core.Domain/Entities/Financeiro/Amizade.cs`
- `Core.Domain/Enums/StatusConviteAmizade.cs`
- Tratamento de erro:
- `Core.Api/Middlewares/ErrorHandlingMiddleware.cs`
- `Core.Api/Extensions/ErroMensagemExtensions.cs`

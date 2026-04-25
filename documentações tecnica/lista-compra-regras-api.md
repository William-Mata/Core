# ListaCompraController - Regras de API

## 1. Indice geral
- Resumo geral:
  - controller responsavel por listas de compra, participantes, itens, acoes em lote e logs.
  - gerenciamento de participantes ocorre no `POST /api/compras/listas` e `PUT /api/compras/listas/{id}`.
  - todos os endpoints exigem JWT Bearer.
- Endpoints atuais (controller):
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
  - `GET /api/compras/listas/{id}/itens/{itemId}`
  - `DELETE /api/compras/listas/{id}/itens/{itemId}`
  - `PUT /api/compras/listas/{id}/itens/{itemId}`
  - `PATCH /api/compras/listas/{id}/itens/{itemId}/edicao-rapida`
  - `POST /api/compras/listas/{id}/itens/{itemId}/marcar-comprado`
  - `POST /api/compras/listas/{id}/acoes-lote`
- Endpoints descontinuados:
  - `POST /api/compras/listas/{id}/participantes`
  - `DELETE /api/compras/listas/{id}/participantes/{participanteId}`

## 2. Regras transversais
- Autenticacao/autorizacao:
  - JWT obrigatorio em todas as rotas.
  - `usuario_nao_autenticado` quando nao ha usuario autenticado.
- Acesso a lista:
  - leitura: proprietario ou participante ativo.
  - edicao: proprietario ou participante ativo com papel diferente de `Leitor`.
  - arquivar/excluir: somente proprietario.
- Integridade de participantes (cadastro/edicao de lista):
  - `usuarioId` invalido: `participante_invalido`.
  - duplicidade de usuario: `participante_duplicado`.
  - deve existir exatamente 1 `Proprietario`: `lista_compra_proprietario_invalido`.
  - cadastro exige proprietario igual ao usuario autenticado.
  - participante nao amigo aceito: `participante_nao_eh_amigo_aceito`.
- Erros funcionais comuns:
  - `lista_compra_nao_encontrada`
  - `lista_compra_sem_permissao_edicao`
  - `lista_compra_sem_permissao_visualizacao`
  - `item_lista_compra_nao_encontrado`
  - `acao_lote_invalida`
- Eventos SignalR publicados:
  - `lista_criada`, `lista_atualizada`, `lista_arquivada`, `lista_excluida`, `lista_duplicada`
  - `item_criado`, `item_atualizado`, `item_edicao_rapida`, `item_comprado`, `item_desmarcado`
  - `lote_executado`

## 3. Contratos completos (request/response)
- Observacao:
  - os exemplos abaixo representam o body completo dos DTOs atuais.
  - enums aparecem como texto para leitura do contrato (`Proprietario`, `Ativa`, `Kg`, etc.).

### 3.1 ParticipanteListaCompraRequest
```json
{
  "usuarioId": 2,
  "papel": "CoProprietario"
}
```

### 3.2 CriarListaCompraRequest
```json
{
  "nome": "Mercado da semana",
  "categoria": "Mercado",
  "observacao": "Compra mensal",
  "participantes": [
    {
      "usuarioId": 1,
      "papel": "Proprietario"
    },
    {
      "usuarioId": 2,
      "papel": "CoProprietario"
    },
    {
      "usuarioId": 3,
      "papel": "Leitor"
    }
  ]
}
```

### 3.3 AtualizarListaCompraRequest
```json
{
  "nome": "Mercado atualizado",
  "categoria": "Casa",
  "observacao": "Replanejada",
  "status": "Ativa",
  "participantes": [
    {
      "usuarioId": 1,
      "papel": "Proprietario"
    },
    {
      "usuarioId": 3,
      "papel": "Leitor"
    }
  ]
}
```

### 3.4 CriarItemListaCompraRequest
```json
{
  "descricao": "Tomate",
  "observacao": "Italiano",
  "unidade": "Kg",
  "quantidade": 2.0,
  "precoUnitario": 10.5,
  "etiquetaCor": "#ff0000"
}
```

### 3.5 AtualizarItemListaCompraRequest
```json
{
  "descricao": "Tomate",
  "observacao": "Italiano",
  "unidade": "Kg",
  "quantidade": 3.0,
  "precoUnitario": 11.0,
  "etiquetaCor": "#00ff00",
  "comprado": true
}
```

### 3.6 EdicaoRapidaItemListaCompraRequest
```json
{
  "quantidade": 2.5,
  "precoUnitario": 9.9
}
```

### 3.7 MarcarCompradoItemListaCompraRequest
```json
{
  "comprado": true
}
```

### 3.8 AcaoLoteListaCompraRequest
```json
{
  "acao": "MarcarSelecionadosComoComprados",
  "itensIds": [101, 102, 103],
  "nomeNovaLista": "Lista derivada",
  "categoriaNovaLista": "Mercado"
}
```

### 3.9 ListaCompraResumoDto (response GET /listas)
```json
{
  "id": 12,
  "nome": "Mercado da semana",
  "categoria": "Mercado",
  "observacao": "Compra mensal",
  "status": "ativa",
  "papelUsuario": "Proprietario",
  "valorTotal": 245.9,
  "valorComprado": 80.0,
  "percentualComprado": 33.33,
  "quantidadeItens": 9,
  "quantidadeItensComprados": 3,
  "quantidadeParticipantes": 2,
  "dataHoraAtualizacao": "2026-04-24T14:00:00Z"
}
```

### 3.10 ListaCompraParticipantesDetalheDto (response GET /listas/{id}/detalhe)
```json
{
  "id": 12,
  "nome": "Mercado da semana",
  "categoria": "Mercado",
  "observacao": "Compra mensal",
  "status": "ativa",
  "participantes": [
    {
      "usuarioId": 1,
      "nome": "William",
      "papel": "Proprietario"
    },
    {
      "usuarioId": 2,
      "nome": "Alex",
      "papel": "CoProprietario"
    }
  ],
  "logs": [
    {
      "id": 1001,
      "dataHoraCadastro": "2026-04-24T14:10:00Z",
      "usuarioCadastroId": 1,
      "itemListaCompraId": 101,
      "acao": "Atualizacao",
      "descricao": "Item atualizado.",
      "valorAnterior": "quantidade=1;preco=8",
      "valorNovo": "quantidade=2;preco=10.5"
    }
  ],
  "dataHoraAtualizacao": "2026-04-24T14:00:00Z"
}
```

### 3.11 ItemListaCompraDto
```json
{
  "id": 101,
  "descricao": "Tomate",
  "observacao": "Italiano",
  "unidade": "Kg",
  "quantidade": 2.0,
  "precoUnitario": 10.5,
  "valorTotal": 21.0,
  "etiquetaCor": "#ff0000",
  "comprado": false,
  "dataHoraCompra": null
}
```

### 3.12 ListaCompraLogDto
```json
{
  "id": 1001,
  "dataHoraCadastro": "2026-04-24T14:10:00Z",
  "usuarioCadastroId": 1,
  "itemListaCompraId": 101,
  "acao": "Atualizacao",
  "descricao": "Item atualizado.",
  "valorAnterior": "quantidade=1;preco=8",
  "valorNovo": "quantidade=2;preco=10.5"
}
```

### 3.13 ParticipanteListaCompraDto
```json
{
  "usuarioId": 1,
  "nome": "William",
  "email": "william@core.com",
  "papel": "proprietario"
}
```

### 3.14 ListaCompraDetalheDto (response completo)
```json
{
  "id": 12,
  "nome": "Mercado da semana",
  "categoria": "Mercado",
  "observacao": "Compra mensal",
  "status": "ativa",
  "valorTotal": 245.9,
  "valorComprado": 80.0,
  "percentualComprado": 33.33,
  "quantidadeItens": 9,
  "quantidadeItensComprados": 3,
  "itens": [
    {
      "id": 101,
      "descricao": "Tomate",
      "observacao": "Italiano",
      "unidade": "Kg",
      "quantidade": 2.0,
      "precoUnitario": 10.5,
      "valorTotal": 21.0,
      "etiquetaCor": "#ff0000",
      "comprado": false,
      "dataHoraCompra": null
    }
  ],
  "participantes": [
    {
      "usuarioId": 1,
      "nome": "William",
      "email": "william@core.com",
      "papel": "proprietario"
    }
  ],
  "logs": [
    {
      "id": 1001,
      "dataHoraCadastro": "2026-04-24T14:10:00Z",
      "usuarioCadastroId": 1,
      "itemListaCompraId": 101,
      "acao": "Atualizacao",
      "descricao": "Item atualizado.",
      "valorAnterior": "quantidade=1;preco=8",
      "valorNovo": "quantidade=2;preco=10.5"
    }
  ],
  "dataHoraAtualizacao": "2026-04-24T14:20:00Z"
}
```

### 3.15 Response de sugestoes (`ItemListaCompraDto`)
```json
{
  "id": 101,
  "descricao": "Tomate",
  "observacao": "Italiano",
  "unidade": "Kg",
  "quantidade": 2.0,
  "precoUnitario": 10.5,
  "valorTotal": 21.0,
  "etiquetaCor": "#ff0000",
  "comprado": false,
  "dataHoraCompra": null
}
```

### 3.16 AcaoLoteListaCompraResultadoDto
```json
{
  "acao": "MarcarSelecionadosComoComprados",
  "itensAfetados": 3,
  "novaListaId": null
}
```

## 4. Endpoints de listas

### 3.1 GET /api/compras/listas
- Objetivo: listar listas acessiveis ao usuario.
- Request:
  - query opcional: `incluirArquivadas` (bool, default `false`).
- Response sucesso:
  - `200 OK` com `IReadOnlyCollection<ListaCompraResumoDto>`.
- Regras:
  - aplica filtro de acesso do usuario autenticado.
- Efeitos colaterais:
  - sem escrita.
- Exemplo:
```bash
curl -X GET "https://api.exemplo.com/api/compras/listas?incluirArquivadas=false" -H "Authorization: Bearer <token>"
```

### 3.2 GET /api/compras/listas/{id}
- Objetivo: obter detalhe completo da lista com itens, participantes e logs.
- Response sucesso:
  - `200 OK` com `ListaCompraDetalheDto`.
- Erros:
  - `404`: `lista_compra_nao_encontrada`.
- Efeitos colaterais:
  - sem escrita.

### 3.3 GET /api/compras/listas/{id}/detalhe
- Objetivo: obter metadados da lista, participantes e logs (sem itens).
- Response sucesso:
  - `200 OK` com `ListaCompraParticipantesDetalheDto`.
- Erros:
  - `404`: `lista_compra_nao_encontrada`.
- Efeitos colaterais:
  - sem escrita.

### 3.4 POST /api/compras/listas
- Objetivo: criar lista.
- Request body:
  - `CriarListaCompraRequest` (`nome`, `categoria`, `observacao`, `participantes` opcional).
- Response sucesso:
  - `201 Created` com `ListaCompraDetalheDto`.
- Regras:
  - valida obrigatoriedade de `nome` e `categoria`.
  - valida regras de participantes (secao 2).
  - se `participantes` nao informado, cria com proprietario autenticado.
- Efeitos colaterais:
  - persiste lista/participantes.
  - cria log de cadastro.
  - publica `lista_criada`.

### 3.5 PUT /api/compras/listas/{id}
- Objetivo: editar dados basicos da lista e participantes.
- Request body:
  - `AtualizarListaCompraRequest` (`nome`, `categoria`, `observacao`, `status` opcional, `participantes` opcional).
- Response sucesso:
  - `200 OK` com `ListaCompraDetalheDto`.
- Regras:
  - exige permissao de edicao (`PodeEditar`).
  - valida nome/categoria.
  - se `participantes` enviado, sincroniza participantes ativos com payload.
  - se `participantes` nao enviado, preserva participantes existentes.
- Efeitos colaterais:
  - atualiza lista/participantes.
  - cria log de atualizacao.
  - publica `lista_atualizada`.

### 3.6 POST /api/compras/listas/{id}/arquivar
- Objetivo: arquivar lista.
- Response sucesso:
  - `200 OK` com `ListaCompraDetalheDto`.
- Regras:
  - somente proprietario.
  - bloqueia dupla arquivacao (`lista_compra_ja_arquivada`).
- Efeitos colaterais:
  - atualiza status para `Arquivada`.
  - cria log.
  - publica `lista_arquivada`.

### 3.7 POST /api/compras/listas/{id}/duplicar
- Objetivo: criar copia de lista existente.
- Request body:
  - `CriarListaCompraRequest` para nome/categoria/observacao da nova lista.
- Response sucesso:
  - `200 OK` com `ListaCompraDetalheDto` da nova lista.
- Regras:
  - exige permissao de edicao na lista origem.
  - copia itens com `Comprado = false` e `DataHoraCompra = null`.
- Efeitos colaterais:
  - persiste nova lista e itens.
  - cria log.
  - publica `lista_duplicada`.

### 3.8 DELETE /api/compras/listas/{id}
- Objetivo: excluir lista.
- Response sucesso:
  - `204 NoContent`.
- Regras:
  - somente proprietario.
- Efeitos colaterais:
  - remove lista.
  - publica `lista_excluida`.

## 5. Endpoints de itens

### 4.1 GET /api/compras/listas/{id}/sugestoes-itens
- Objetivo: buscar sugestoes de itens por descricao parcial para autocomplete.
- Request:
  - query opcional: `descricao`.
- Response sucesso:
  - `200 OK` com `IReadOnlyCollection<ItemListaCompraDto>`.
- Regras:
  - se `descricao` tiver menos de 3 caracteres, retorna colecao vazia.
  - busca parcial por descricao (contains/like) sem exigir match exato.
  - retorna multiplos registros.
  - retorna dados completos do item sugerido.
  - retorna apenas itens de listas que o usuario tem acesso (proprietario ou participante ativo).
- Efeitos colaterais:
  - sem escrita.

### 4.2 POST /api/compras/listas/{id}/itens
- Objetivo: adicionar item na lista.
- Request body:
  - `CriarItemListaCompraRequest`.
- Response sucesso:
  - `200 OK` com `ItemListaCompraDto`.
- Regras:
  - exige permissao de edicao.
  - valida descricao obrigatoria e quantidade > 0.
- Efeitos colaterais:
  - persiste item.
  - pode criar/atualizar produto e historico de preco.
  - cria log.
  - publica `item_criado`.

### 4.3 GET /api/compras/listas/{id}/itens/{itemId}
- Objetivo: consultar item completo por identificador.
- Response sucesso:
  - `200 OK` com `ItemListaCompraDto`.
- Regras:
  - exige acesso de visualizacao a lista.
  - retorna apenas um item.
- Erros:
  - `404`: `lista_compra_nao_encontrada` ou `item_lista_compra_nao_encontrado`.
- Efeitos colaterais:
  - sem escrita.

### 4.4 DELETE /api/compras/listas/{id}/itens/{itemId}
- Objetivo: excluir item por identificador.
- Response sucesso:
  - `204 NoContent`.
- Regras:
  - exige permissao de edicao.
  - item inexistente retorna `item_lista_compra_nao_encontrado`.
- Efeitos colaterais:
  - remove item da lista.
  - cria log de exclusao.
  - publica `item_excluido`.

### 4.5 PUT /api/compras/listas/{id}/itens/{itemId}
- Objetivo: atualizar item completo.
- Request body:
  - `AtualizarItemListaCompraRequest`.
- Response sucesso:
  - `200 OK` com `ItemListaCompraDto`.
- Regras:
  - exige permissao de edicao.
  - valida item existente, descricao obrigatoria e quantidade > 0.
- Efeitos colaterais:
  - atualiza item/produto/historico quando aplicavel.
  - cria log.
  - publica `item_atualizado`.

### 4.6 PATCH /api/compras/listas/{id}/itens/{itemId}/edicao-rapida
- Objetivo: atualizar quantidade e preco do item.
- Request body:
  - `EdicaoRapidaItemListaCompraRequest`.
- Response sucesso:
  - `200 OK` com `ItemListaCompraDto`.
- Regras:
  - exige permissao de edicao.
  - valida quantidade > 0.
- Efeitos colaterais:
  - atualiza item.
  - registra historico de preco quando aplicavel.
  - cria log.
  - publica `item_edicao_rapida`.

### 4.7 POST /api/compras/listas/{id}/itens/{itemId}/marcar-comprado
- Objetivo: marcar/desmarcar item como comprado.
- Request body:
  - `MarcarCompradoItemListaCompraRequest` (`comprado`).
- Response sucesso:
  - `200 OK` com `ItemListaCompraDto`.
- Regras:
  - exige permissao de edicao.
- Efeitos colaterais:
  - atualiza status de compra e data de compra.
  - pode registrar historico de preco quando marcado.
  - cria log.
  - publica `item_comprado` ou `item_desmarcado`.

### 4.8 POST /api/compras/listas/{id}/acoes-lote
- Objetivo: executar acao em lote sobre itens/lista.
- Request body:
  - `AcaoLoteListaCompraRequest`:
    - `acao`:
      - `MarcarSelecionadosComoComprados`
      - `DesmarcarSelecionados`
      - `ExcluirSelecionados`
      - `ExcluirComprados`
      - `ExcluirNaoComprados`
      - `ExcluirSemPreco`
      - `LimparLista`
      - `ResetarPrecos`
      - `ResetarCores`
      - `CriarNovaListaComComprados`
      - `CriarNovaListaComNaoComprados`
      - `DuplicarLista`
      - `MesclarDuplicados`
    - `itensIds`, `nomeNovaLista`, `categoriaNovaLista` opcionais.
- Response sucesso:
  - `200 OK` com `AcaoLoteListaCompraResultadoDto`.
- Regras:
  - exige permissao de edicao.
  - acao invalida retorna `acao_lote_invalida`.
- Efeitos colaterais:
  - altera itens/lista conforme acao.
  - pode criar nova lista.
  - cria log.
  - publica `lote_executado`.

## 6. Matriz de erros por endpoint
| Endpoint | 400 | 401 | 404 |
|---|---|---|---|
| `GET /listas` | regras de dominio quando aplicavel | token invalido/ausente | - |
| `GET /listas/{id}` | - | token invalido/ausente | `lista_compra_nao_encontrada` |
| `GET /listas/{id}/detalhe` | - | token invalido/ausente | `lista_compra_nao_encontrada` |
| `POST /listas` | validacoes de lista/participantes | token invalido/ausente | - |
| `PUT /listas/{id}` | validacoes/permissao edicao | token invalido/ausente | `lista_compra_nao_encontrada` |
| `POST /listas/{id}/arquivar` | `lista_compra_ja_arquivada` | token invalido/ausente | `lista_compra_nao_encontrada` |
| `POST /listas/{id}/duplicar` | validacoes/permissao | token invalido/ausente | `lista_compra_nao_encontrada` |
| `DELETE /listas/{id}` | - | token invalido/ausente | `lista_compra_nao_encontrada` |
| `GET /listas/{id}/sugestoes-itens` | `lista_compra_sem_permissao_visualizacao` | token invalido/ausente | `lista_compra_nao_encontrada` |
| `POST /listas/{id}/itens` | validacoes de item/permissao | token invalido/ausente | `lista_compra_nao_encontrada` |
| `GET /listas/{id}/itens/{itemId}` | `lista_compra_sem_permissao_visualizacao` | token invalido/ausente | lista/item nao encontrado |
| `DELETE /listas/{id}/itens/{itemId}` | permissao de edicao | token invalido/ausente | lista/item nao encontrado |
| `PUT /listas/{id}/itens/{itemId}` | validacoes de item/permissao | token invalido/ausente | lista/item nao encontrado |
| `PATCH /listas/{id}/itens/{itemId}/edicao-rapida` | validacoes de item/permissao | token invalido/ausente | lista/item nao encontrado |
| `POST /listas/{id}/itens/{itemId}/marcar-comprado` | permissao | token invalido/ausente | lista/item nao encontrado |
| `POST /listas/{id}/acoes-lote` | permissao/acao invalida | token invalido/ausente | `lista_compra_nao_encontrada` |

## 7. Exemplo de erro esperado
```bash
curl -X PUT "https://api.exemplo.com/api/compras/listas/12" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"nome":"","categoria":"Mercado"}'
```
Resultado esperado: erro de regra de negocio (`lista_compra_nome_obrigatorio`).

## 8. Rastreabilidade no codigo
- Controller: `Core.Api/Controllers/Compras/ListaCompraController.cs`
- Service: `Core.Application/Services/Compras/ComprasService.cs`
- DTOs: `Core.Application/DTOs/Compras/ComprasDtos.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Compras/ComprasRepository.cs`
- Enums: `Core.Domain/Enums/Compras/*`
- Testes: `Core.Tests/Unit/Application/ComprasServiceTests.cs`

## 9. Fatos confirmados e inferencias
### Fatos confirmados
- todos os endpoints listados no indice existem atualmente na `ListaCompraController`.
- endpoints dedicados de participantes nao existem mais na controller.
- `POST` e `PUT` de lista aceitam participantes no contrato atual.

### Inferencias
- o mapeamento final de `DomainException`/`NotFoundException` para HTTP ocorre no middleware global de erro.

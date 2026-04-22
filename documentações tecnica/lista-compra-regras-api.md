# ListaCompraController - Regras de API

## Objetivo
Documentar o contrato de listas de compras, itens, compartilhamento e logs do usuario autenticado.

## Autenticacao
- Todas as rotas exigem JWT Bearer.

## Rotas
- `GET /api/compras/listas`
- `GET /api/compras/listas/{id}`
- `POST /api/compras/listas`
- `PUT /api/compras/listas/{id}`
- `POST /api/compras/listas/{id}/arquivar`
- `POST /api/compras/listas/{id}/duplicar`
- `DELETE /api/compras/listas/{id}`
- `POST /api/compras/listas/{id}/participantes`
- `DELETE /api/compras/listas/{id}/participantes/{participanteId}`
- `GET /api/compras/listas/{id}/sugestoes-itens`
- `POST /api/compras/listas/{id}/itens`
- `PUT /api/compras/listas/{id}/itens/{itemId}`
- `PATCH /api/compras/listas/{id}/itens/{itemId}/edicao-rapida`
- `POST /api/compras/listas/{id}/itens/{itemId}/marcar-comprado`
- `POST /api/compras/listas/{id}/acoes-lote`
- `GET /api/compras/listas/{id}/logs`

## Atualizacao em tempo real (SignalR)
- Hub: `GET /hubs/compras`
- Autenticacao no hub: JWT Bearer (via `access_token` na query string da conexao WebSocket).
- Metodos do hub:
  - `EntrarLista(listaId)`: adiciona a conexao ao grupo da lista.
  - `SairLista(listaId)`: remove a conexao do grupo da lista.
- Evento enviado pelo servidor:
  - `listaAtualizada`
- Payload do evento:
```json
{
  "listaId": 12,
  "evento": "item_atualizado",
  "usuarioId": 1,
  "dataHoraUtc": "2026-04-21T18:30:00Z"
}
```
- Eventos publicados atualmente:
  - `lista_criada`
  - `lista_atualizada`
  - `lista_arquivada`
  - `lista_excluida`
  - `lista_duplicada`
  - `lista_compartilhada`
  - `participante_removido`
  - `item_criado`
  - `item_atualizado`
  - `item_edicao_rapida`
  - `item_comprado`
  - `item_desmarcado`
  - `lote_executado`
  - `desejos_convertidos`
  - `lista_derivada_criada`

## Regras globais
- A lista e sempre carregada no contexto do usuario autenticado (proprietario ou participante ativo).
- O proprietario controla compartilhamento, remocao de participante, arquivamento e exclusao da lista.
- O papel `Editor` altera itens e executa lote; `Leitor` apenas consulta.
- `ValorTotal` do item e derivado de `Quantidade x PrecoUnitario`.
- `PercentualComprado` e calculado por quantidade de itens concluidos.
- Toda alteracao relevante gera rastreabilidade em `ListaCompraLog`.

## Validacoes principais
- `usuario_nao_autenticado`: usuario sem contexto autenticado.
- `lista_compra_nao_encontrada`: lista inexistente ou sem acesso.
- `lista_compra_sem_permissao_edicao`: usuario sem permissao de escrita.
- `lista_compra_sem_permissao_visualizacao`: usuario sem permissao de leitura.
- `lista_compra_ja_arquivada`: tentativa de arquivar lista ja arquivada.
- `item_lista_compra_descricao_obrigatoria`: descricao do item obrigatoria.
- `quantidade_item_invalida`: quantidade deve ser maior que zero.
- `participante_invalido`: amigo invalido (inclui compartilhar com o proprio usuario).
- `participante_nao_eh_amigo_aceito`: compartilhamento apenas com amizade aceita.
- `nao_permitido_remover_proprietario`: bloqueio para remover o dono da lista.
- `participante_nao_encontrado`: participante nao localizado na lista.
- `acao_lote_invalida`: acao de lote fora do enum esperado.

## Regras de autocomplete e preco
- Sugestoes de item somente quando `descricao` tiver pelo menos 3 caracteres.
- Busca de sugestao considera produto utilizado pelo usuario e produtos de listas em que participa.
- Ao vincular produto existente, unidade/observacao/preco podem ser reaproveitados.
- Historico de preco e registrado ao informar preco valido e ao confirmar compra.

## Acoes de lote suportadas
- Marcar selecionados como comprados.
- Desmarcar selecionados.
- Excluir selecionados.
- Excluir comprados.
- Excluir nao comprados.
- Excluir sem preco.
- Limpar lista.
- Resetar precos.
- Resetar cores.
- Criar nova lista com comprados.
- Criar nova lista com nao comprados.
- Duplicar lista.
- Mesclar duplicados.

## Exemplo de resposta (GET detalhe)
```json
{
  "id": 12,
  "nome": "Mercado da semana",
  "categoria": "Mercado",
  "status": "ativa",
  "valorTotal": 245.90,
  "valorComprado": 80.00,
  "percentualComprado": 33.33,
  "quantidadeItens": 9,
  "quantidadeItensComprados": 3,
  "itens": [],
  "participantes": [],
  "logs": []
}
```

## Erros comuns
- `dados_invalidos`
- `usuario_nao_autenticado`
- `lista_compra_nao_encontrada`
- `lista_compra_sem_permissao_edicao`
- `item_lista_compra_nao_encontrado`
- `participante_nao_eh_amigo_aceito`

## Rastreabilidade
- Controller: `Core.Api/Controllers/Compras/ListaCompraController.cs`
- Service: `Core.Application/Services/Compras/ComprasService.cs`
- DTOs: `Core.Application/DTOs/Compras/ComprasDtos.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Compras/ComprasRepository.cs`
- Entidades: `Core.Domain/Entities/Compras/*`

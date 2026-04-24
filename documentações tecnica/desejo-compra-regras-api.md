# DesejoCompraController - Regras de API

## Objetivo
Documentar o contrato de gerenciamento de desejos de compra e conversao para lista de compras.

## Autenticacao
- Todas as rotas exigem JWT Bearer.

## Rotas
- `GET /api/compras/desejos`
- `POST /api/compras/desejos`
- `PUT /api/compras/desejos/{id}`
- `DELETE /api/compras/desejos/{id}`
- `POST /api/compras/desejos/converter`

## Permissoes de funcionalidade (tela)
- Tela: `Desejos` (modulo `Compras`)
- Funcionalidades padrao ativas:
  - `Visualizar`
  - `Criar`
  - `Editar`
  - `Excluir`

## Regras globais
- Desejo pertence ao usuario autenticado.
- Desejo nao impacta totais de lista ate ser convertido.
- Conversao pode criar lista nova ou adicionar itens em lista existente.
- Pos-conversao suporta:
  - manter desejo
  - arquivar (remover registro)
  - marcar como convertido

## Validacoes principais
- `usuario_nao_autenticado`
- `desejo_compra_descricao_obrigatoria`
- `quantidade_item_invalida`
- `desejo_compra_nao_encontrado`
- `desejos_nao_informados`
- `desejos_nao_encontrados`
- `lista_compra_nao_encontrada`
- `lista_compra_sem_permissao_edicao`

## Regras de conversao
- `DesejosIds` e obrigatorio e sem duplicidade logica.
- Se `ListaDestinoId` for informado:
  - lista deve existir e ser acessivel
  - usuario deve ter permissao de edicao
- Se `ListaDestinoId` nao for informado:
  - nova lista e criada com nome/categoria informados ou valores default
  - usuario autenticado vira proprietario
- Cada desejo vira um `ItemListaCompra` com:
  - descricao e descricao normalizada
  - observacao
  - unidade
  - quantidade
  - preco estimado como preco unitario inicial

## Exemplo de request (converter)
```json
{
  "desejosIds": [10, 11, 15],
  "listaDestinoId": null,
  "nomeNovaLista": "Compras do mes",
  "categoriaNovaLista": "Mercado",
  "acaoPosConversao": "MarcarComoConvertido"
}
```

## Exemplo de response (converter)
```json
{
  "listaId": 44,
  "itensCriados": 3,
  "desejosProcessados": 3
}
```

## Erros comuns
- `dados_invalidos`
- `usuario_nao_autenticado`
- `desejo_compra_nao_encontrado`
- `desejos_nao_informados`
- `lista_compra_sem_permissao_edicao`

## Rastreabilidade
- Controller: `Core.Api/Controllers/Compras/DesejoCompraController.cs`
- Service: `Core.Application/Services/Compras/ComprasService.cs`
- DTOs: `Core.Application/DTOs/Compras/ComprasDtos.cs`
- Repository: `Core.Infrastructure/Persistence/Repositories/Compras/ComprasRepository.cs`
- Entidades: `Core.Domain/Entities/Compras/DesejoCompra.cs`

# Scripts organizados por tabela - Compras

Ordem de execução recomendada:

1. `10-lista-compra.sql`
2. `11-produto.sql`
3. `12-item-lista-compra.sql`
4. `13-participacao-lista-compra.sql`
5. `14-desejo-compra.sql`
6. `15-historico-produto.sql`
7. `16-lista-compra-log.sql`

## Dependências principais

- `ItemListaCompra` depende de `ListaCompra` e `Produto`.
- `ParticipacaoListaCompra` depende de `ListaCompra` e `Usuario`.
- `DesejoCompra` depende de `Produto` e `Usuario`.
- `HistoricoProduto` depende de `Produto` e `ItemListaCompra`.
- `ListaCompraLog` depende de `ListaCompra` e `ItemListaCompra`.

## Observações

- As ações em `Modulo`, `Tela`, `Funcionalidade` e permissões de usuário foram movidas para os SQLs das respectivas tabelas em `01-seguranca/organizado`.
- `00-permissoes-compras.sql` foi mantido apenas por compatibilidade histórica, sem ações em tabela.

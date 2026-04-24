# Ordem de execução dos scripts organizados

## 1) Segurança
Executar nesta ordem:

1. `query''s/01-seguranca/organizado/10-usuario.sql`
2. `query''s/01-seguranca/organizado/11-modulo.sql`
3. `query''s/01-seguranca/organizado/12-tela.sql`
4. `query''s/01-seguranca/organizado/13-funcionalidade.sql`
5. `query''s/01-seguranca/organizado/14-usuario-modulo.sql`
6. `query''s/01-seguranca/organizado/15-usuario-tela.sql`
7. `query''s/01-seguranca/organizado/16-usuario-funcionalidade.sql`
8. `query''s/01-seguranca/organizado/17-refresh-token.sql`
9. `query''s/01-seguranca/organizado/18-tentativa-login-invalida.sql`

## 2) Cadastro
Executar:

1. `query''s/02-cadastro/02-area-subarea.sql`
2. `query''s/02-cadastro/03-seed-area-subarea-financeiro.sql`

## 3) Financeiro
Executar nesta ordem:

1. `query''s/03-financeiro/organizado/10-conta-bancaria.sql`
2. `query''s/03-financeiro/organizado/11-conta-bancaria-extrato.sql`
3. `query''s/03-financeiro/organizado/12-conta-bancaria-log.sql`
4. `query''s/03-financeiro/organizado/13-cartao.sql`
5. `query''s/03-financeiro/organizado/14-cartao-log.sql`
6. `query''s/03-financeiro/organizado/15-despesa.sql`
7. `query''s/03-financeiro/organizado/16-despesa-amigo-rateio.sql`
8. `query''s/03-financeiro/organizado/17-despesa-area-rateio.sql`
9. `query''s/03-financeiro/organizado/18-despesa-tipo-rateio.sql`
10. `query''s/03-financeiro/organizado/19-despesa-log.sql`
11. `query''s/03-financeiro/organizado/20-receita.sql`
12. `query''s/03-financeiro/organizado/21-receita-amigo-rateio.sql`
13. `query''s/03-financeiro/organizado/22-receita-area-rateio.sql`
14. `query''s/03-financeiro/organizado/23-receita-log.sql`
15. `query''s/03-financeiro/organizado/24-reembolso.sql`
16. `query''s/03-financeiro/organizado/25-reembolso-despesa.sql`
17. `query''s/03-financeiro/organizado/26-historico-transacao-financeira.sql`
18. `query''s/03-financeiro/organizado/27-documento.sql`
19. `query''s/03-financeiro/organizado/28-convite-amizade.sql`
20. `query''s/03-financeiro/organizado/29-amizade.sql`
21. `query''s/03-financeiro/organizado/30-fatura-cartao.sql`

## 4) Compras
Executar nesta ordem:

1. `query''s/04-compras/organizado/10-lista-compra.sql`
2. `query''s/04-compras/organizado/11-produto.sql`
3. `query''s/04-compras/organizado/12-item-lista-compra.sql`
4. `query''s/04-compras/organizado/13-participacao-lista-compra.sql`
5. `query''s/04-compras/organizado/14-desejo-compra.sql`
6. `query''s/04-compras/organizado/15-historico-produto.sql`
7. `query''s/04-compras/organizado/16-lista-compra-log.sql`

## Observações

- Qualquer ação em tabela foi movida para o SQL da respectiva tabela.
- Os arquivos `00-seeds-e-permissoes.sql` e `00-permissoes-compras.sql` foram mantidos apenas por compatibilidade histórica (sem ação em tabela).

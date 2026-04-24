# Scripts organizados por tabela - Segurança

Ordem de execução recomendada:

1. `10-usuario.sql`
2. `11-modulo.sql`
3. `12-tela.sql`
4. `13-funcionalidade.sql`
5. `14-usuario-modulo.sql`
6. `15-usuario-tela.sql`
7. `16-usuario-funcionalidade.sql`
8. `17-refresh-token.sql`
9. `18-tentativa-login-invalida.sql`

## Dependências principais

- `Modulo` depende de `Usuario`.
- `Tela` depende de `Modulo` e `Usuario`.
- `Funcionalidade` depende de `Tela` e `Usuario`.
- `UsuarioModulo`, `UsuarioTela` e `UsuarioFuncionalidade` dependem de permissões base.
- `RefreshToken` e `TentativaLoginInvalida` dependem de `Usuario`.

## Observações

- Toda ação de `INSERT/UPDATE` foi distribuída para o SQL da própria tabela.
- `00-seeds-e-permissoes.sql` foi mantido apenas por compatibilidade histórica, sem ações em tabela.

# Tela de Usuario

## Objetivo
Documentar o contrato atual do front-end da tela de usuario para integracao com API.

Arquivo principal:
- `app/principal/usuario.tsx`

## Rotas do front
- tela do painel do usuario: `/principal/usuario`
- botao fechar da tela: redireciona para `/principal`

## Blocos da tela
- Informacoes do usuario logado:
  - nome
  - email
  - perfil de acesso
- Alteracao de senha:
  - senha atual
  - nova senha
  - confirmar nova senha

## Origem dos dados exibidos
- dados de identificacao do usuario:
  - `usarAutenticacaoStore().usuario`
- alteracao de senha:
  - servico `alterarSenha` em `src/servicos/autenticacao.ts`

## Validacoes aplicadas no front
- todos os campos de senha sao obrigatorios:
  - `senhaAtual`
  - `novaSenha`
  - `confirmarSenha`
- `novaSenha` deve ter no minimo `10` caracteres
- `confirmarSenha` deve ser igual a `novaSenha`

Quando a validacao falha:
- campo invalido recebe destaque visual (borda de erro)
- o front mostra mensagem em tela (toast), sem popup nativo

## Endpoint esperado para alterar senha
- `POST /api/autenticacao/alterar-senha`

Payload:

```json
{
  "senhaAtual": "SenhaAtual@123",
  "novaSenha": "NovaSenha@123",
  "confirmarSenha": "NovaSenha@123"
}
```

## Resposta esperada em sucesso
- `200` ou `204`
- sem necessidade de payload especifico
- o front limpa os campos e mostra mensagem de sucesso

Observacao:
- quando a API retornar objeto com `mensagem`, o front pode exibir esse texto.

## Resposta esperada em erro
Padrao principal esperado:
- RFC 7807 (`application/problem+json`)

Exemplo:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Requisicao invalida",
  "status": 400,
  "detail": "Os dados informados sao invalidos.",
  "instance": "/api/autenticacao/alterar-senha",
  "errors": {
    "novaSenha": [
      "A senha deve ter no minimo 10 caracteres."
    ]
  }
}
```

Regras do front:
- se existir `detail`, exibir `detail`
- se existir lista de erros (`errors`), consolidar mensagens para exibicao
- nao usar `Alert.alert`; erro deve aparecer no sistema de notificacao em tela

## Comportamento de carregamento
- ao enviar alteracao de senha:
  - botao fica desabilitado
  - texto do botao muda para estado de carregando
  - evita clique duplo e reenvio concorrente

## Integracao de autenticacao
- esta tela depende de sessao autenticada valida
- tokens (`accessToken` e `refreshToken`) ja devem existir no fluxo de login
- em caso de token expirado, o interceptor do cliente HTTP deve tentar renovacao

## Testes recomendados para esta tela
1. Deve renderizar nome, email e perfil do usuario autenticado.
2. Deve bloquear envio quando algum campo obrigatorio de senha estiver vazio.
3. Deve bloquear envio quando `novaSenha` tiver menos de 10 caracteres.
4. Deve bloquear envio quando confirmacao for diferente.
5. Deve enviar payload correto ao endpoint de alteracao de senha.
6. Deve limpar campos e mostrar sucesso apos resposta de sucesso.
7. Deve exibir `detail` quando a API responder RFC 7807.
8. Deve manter botao desabilitado durante envio.

## Dependencias de traducao (i18n)
Chaves usadas:
- `usuarioPainel.titulo`
- `usuarioPainel.informacoes`
- `usuarioPainel.nome`
- `usuarioPainel.email`
- `usuarioPainel.perfil`
- `usuarioPainel.alterarSenha`
- `usuarioPainel.campos.senhaAtual`
- `usuarioPainel.campos.novaSenha`
- `usuarioPainel.campos.confirmarSenha`
- `usuarioPainel.botaoSalvar`
- `usuarioPainel.sucesso.senhaAlterada`
- `usuarioPainel.erros.camposObrigatorios`
- `usuarioPainel.erros.senhaMinima`
- `usuarioPainel.erros.confirmacaoDiferente`
- `usuarioPainel.erros.falhaAlterarSenha`


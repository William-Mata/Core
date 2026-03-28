# Tela de Login

## Objetivo
Documentar o contrato atual do front-end da tela de login para integracao com API.

Arquivo principal:
- `app/auth/entrar.tsx`

## Rotas do front
- tela de login: `/auth/entrar`
- destino apos autenticacao com sucesso: `/principal`

## Campos da tela
- `email`: obrigatorio
- `senha`: obrigatorio

## Validacoes aplicadas no front
- `email` obrigatorio
- `senha` obrigatoria
- `email` com validacao de formato

Regex atual usada pelo front:

```txt
/^[^\s@]+@[^\s@]+\.[^\s@]+$/
```

## Regra de bloqueio antes do envio
O front nao deve chamar autenticacao quando:
- `email` estiver vazio
- `senha` estiver vazia
- `email` estiver com formato invalido
- o login estiver bloqueado por tentativas invalidas

## Regra de bloqueio por tentativas
- o front contabiliza tentativas com credenciais invalidas
- limite atual: `5`
- ao atingir a quinta tentativa invalida:
  - o botao de login fica bloqueado
  - a tela informa que o login foi bloqueado
- no estado atual, a acao `Esqueci minha senha` reinicia o bloqueio e zera as tentativas

## Payload esperado para autenticacao
Endpoint esperado:
- `POST /api/autenticacao/entrar`

Payload:

```json
{
  "email": "usuario@empresa.com",
  "senha": "123456"
}
```

## Resposta esperada em caso de sucesso
O contrato atual esperado pelo front e:

```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiracao": "2026-03-24T12:00:00.000Z",
  "usuario": {
    "id": 1,
    "nome": "Usuario",
    "email": "usuario@empresa.com",
    "perfil": {
      "id": "ADMIN",
      "nome": "Administrador"
    },
    "modulosAtivos": [
      {
        "id": "financeiro",
        "nome": "Financeiro",
        "status": true,
        "funcionalidades": [
          { "id": "despesa", "nome": "Despesas", "status": true },
          { "id": "receita", "nome": "Receitas", "status": true }
        ]
      }
    ]
  }
}
```

## Estrutura esperada de perfil
- `perfil.id`: identificador do perfil
- `perfil.nome`: nome exibivel do perfil

Regras:
- o front nao espera mais `perfil` como string
- o front consome `perfil.id` e `perfil.nome`

## Estrutura esperada de modulos
- `modulosAtivos` deve ser uma lista de objetos
- cada modulo deve conter:
  - `id`
  - `nome`
  - `funcionalidades`
  - `status`

Cada funcionalidade deve conter:
- `id`
- `nome`
- `status`

Exemplo:

```json
{
  "id": "administracao",
  "nome": "Administracao",
  "status": true,
  "funcionalidades": [
    { "id": "usuarios", "nome": "Usuarios", "status": true },
    { "id": "permissoes", "nome": "Permissoes", "status": false }
  ]
}
```

## Regras do front para modulos e funcionalidades
- o menu lateral verifica acesso por `modulo.id`
- os subitens do menu verificam acesso por `funcionalidade.id`
- se um modulo estiver com `status = false`, o modulo nao deve ser exibido
- se uma funcionalidade estiver com `status = false`, o item correspondente nao deve ser exibido

Mapeamento atual de funcionalidades esperadas pelo menu:

### Modulo `financeiro`
- `despesa`
- `receita`
- `reembolso`
- `conta-bancaria`
- `cartao`
- `documentacao`

### Modulo `amigos`
- `lista`
- `convite`
- `documentacao`

### Modulo `administracao`
- `visao-geral`
- `usuarios`
- `permissoes`
- `documentos`
- `avisos`
- `documentacao`

## Regras de comportamento do front em caso de sucesso
- chamar `definirSessao(usuario, accessToken)`
- marcar sessao autenticada
- navegar para `/principal`

## Regras de comportamento durante o envio
- botao de login entra em estado de carregamento
- campos ficam bloqueados
- botao fica desabilitado

## Credenciais de teste do fluxo mockado atual
No estado atual do front, o fluxo de sucesso local considera:

```json
{
  "email": "admin@core.com",
  "senha": "123456"
}
```

Qualquer outra combinacao valida no formato, mas diferente dessas credenciais, e tratada como credencial invalida para fins de simulacao do login.

## Recuperacao de senha
Fluxo atual da tela:
- existe acao `Esqueci minha senha`
- o front exige `email` preenchido
- o front exige `email` com formato valido
- no estado atual, a tela exibe mensagem de sucesso simulada
- no estado atual, a tela tambem reinicia o contador de tentativas e remove o bloqueio local

Mensagem atual:
- "Se o email estiver cadastrado, as instrucoes de recuperacao serao enviadas."

### Payload sugerido para integracao futura
Endpoint sugerido:
- `POST /api/autenticacao/esqueci-senha`

Payload:

```json
{
  "email": "usuario@empresa.com"
}
```

## Renovacao de token
Ja existe suporte no cliente HTTP:
- endpoint esperado: `POST /autenticacao/renovar`
- arquivo: `src/servicos/api.ts`

Payload esperado:

```json
{
  "refreshToken": "string"
}
```
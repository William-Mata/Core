# Tela de Administracao - Usuarios

## Objetivo
Documentar o comportamento atual do front-end da tela de usuarios do modulo de administracao e o contrato recomendado para integracao com API.

Arquivo principal:
- `app/principal/admin/usuario.tsx`

## Rotas do front
- lista de usuarios: `/principal/admin/usuario`
- novo usuario: `/principal/admin/usuario?novo=1`
- editar usuario: `/principal/admin/usuario?id={id}`

## Modos da tela
- Modo lista:
  - exibe botao para criar novo
  - exibe filtro padrao (id, descricao, data inicio e data fim)
  - exibe listagem com nome, email, data e perfil
- Modo formulario:
  - usado para criacao (`?novo=1`)
  - usado para edicao (`?id={id}`)

## Campos do formulario
- `nome` (obrigatorio)
- `email` (obrigatorio, formato valido)
- `perfil` (select):
  - `USER`
  - `ADMIN`

## Validacoes de front aplicadas
- nome e email obrigatorios
- email validado por regex:

```txt
/^[^\s@]+@[^\s@]+\.[^\s@]+$/
```

- destaque visual de campo invalido para obrigatorios
- mensagens de erro/sucesso em notificacao de tela (sem popup nativo)

## Regras de comportamento
- criar:
  - gera id incremental local
  - define `dataCriacao` com data atual
- editar:
  - altera nome, email e perfil do usuario selecionado
- deletar:
  - disponivel apenas em modo edicao
  - remove usuario selecionado da lista local
- perfil ADMIN:
  - exibe aviso visual de privilegios elevados

## Estado atual da implementacao
- no estado atual, a tela opera com estado local (`useState`)
- nao ha consumo de API real nessa tela
- operacoes de salvar usam simulacao com `setTimeout`

## Contrato recomendado para API (.NET)

### 1) Listar usuarios
- `GET /api/admin/usuarios`

Resposta esperada:

```json
{
  "sucesso": true,
  "dados": [
    {
      "id": 1,
      "nome": "Admin User",
      "email": "admin@example.com",
      "perfil": "ADMIN",
      "data_criacao": "2024-01-01"
    }
  ],
  "quantidade": 1
}
```

### 2) Obter usuario por id
- `GET /api/admin/usuarios/{id}`

Resposta esperada:

```json
{
  "sucesso": true,
  "dados": {
    "id": 1,
    "nome": "Admin User",
    "email": "admin@example.com",
    "perfil": "ADMIN",
    "data_criacao": "2024-01-01"
  }
}
```

### 3) Criar usuario
- `POST /api/admin/usuarios`

Payload:

```json
{
  "nome": "Novo Usuario",
  "email": "novo@empresa.com",
  "perfil": "USER"
}
```

Resposta esperada:

```json
{
  "sucesso": true,
  "mensagem": "Usuario criado com sucesso",
  "dados": {
    "id": 10,
    "nome": "Novo Usuario",
    "email": "novo@empresa.com",
    "perfil": "USER",
    "data_criacao": "2026-03-25T12:00:00Z"
  }
}
```

### 4) Atualizar usuario
- `PUT /api/admin/usuarios/{id}`

Payload:

```json
{
  "nome": "Nome Atualizado",
  "email": "atualizado@empresa.com",
  "perfil": "ADMIN"
}
```

Resposta esperada:

```json
{
  "sucesso": true,
  "mensagem": "Usuario atualizado com sucesso"
}
```

### 5) Excluir usuario
- `DELETE /api/admin/usuarios/{id}`

Resposta esperada:

```json
{
  "sucesso": true,
  "mensagem": "Usuario removido com sucesso"
}
```

## Tratamento de erro esperado
- padrao RFC 7807
- regra do front:
  - se existir `detail`, exibir `detail`
  - se existir `mensagem` em sucesso, exibir `mensagem`

Exemplo RFC 7807:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Requisicao invalida",
  "status": 400,
  "detail": "O email informado ja esta em uso.",
  "instance": "/api/admin/usuarios"
}
```

## Filtro da listagem
- `id`: filtro textual por id
- `descricao`: filtra por nome ou email
- `dataInicio` e `dataFim`: filtra por intervalo de `dataCriacao`

## Dependencias de traducao (i18n)
Principais chaves:
- `admin.usuarios.lista`
- `admin.usuarios.criar`
- `admin.usuarios.vazio`
- `admin.usuario.novo`
- `admin.usuario.editar`
- `admin.usuario.nomeCompleto`
- `admin.usuario.nomePlaceholder`
- `admin.usuario.perfilAcesso`
- `admin.usuario.perfilUser`
- `admin.usuario.perfilAdmin`
- `admin.usuario.privilegiosTitulo`
- `admin.usuario.privilegiosTexto`
- `admin.usuario.salvando`
- `admin.usuario.deletar`
- `admin.usuario.sucessoDeletado`
- `admin.usuario.sucessoSalvo`
- `admin.usuario.criado`
- `admin.usuario.atualizado`
- `admin.usuario.erros.nomeEmailObrigatorio`
- `admin.usuario.erros.emailInvalido`

## Testes recomendados
1. Deve renderizar lista de usuarios e abrir formulario em `?novo=1`.
2. Deve abrir edicao com dados ao acessar `?id={id}`.
3. Deve bloquear salvar sem nome e email.
4. Deve bloquear salvar com email invalido.
5. Deve criar usuario novo com dados validos.
6. Deve atualizar usuario existente em modo edicao.
7. Deve exibir botao de deletar apenas na edicao.
8. Deve deletar usuario selecionado e retornar para lista.
9. Deve aplicar filtros por id, descricao e intervalo de data.


---
name: semantic-commit
description: Criar commits Git com semântica convencional, staging seguro e validação prévia, sempre com mensagens em PT-BR. Usar quando for necessário organizar alterações locais em commits claros, rastreáveis e sem risco de incluir mudanças indevidas.
---

# Semantic Commit

Seguir este fluxo para garantir commits seguros, rastreáveis e consistentes.

---

## 1) Inspeção obrigatória do escopo

Antes de qualquer ação:

- git status --short
- git diff --stat
- git diff --cached --stat

Objetivo:
- entender todos os arquivos alterados
- identificar o que pertence à task atual

---

## 2) Regra crítica de staging (OBRIGATÓRIO)

Fazer stage APENAS dos arquivos que:

- fazem parte direta da tarefa solicitada
- estão relacionados ao mesmo objetivo funcional

Nunca incluir:

- .env, secrets, tokens
- arquivos locais ou temporários
- logs, caches ou builds
- alterações não relacionadas à task

Se houver arquivos fora do escopo:
→ ignorar completamente no commit

---

## 3) Separação de commits

Se existirem mudanças de naturezas diferentes:

- separar em commits distintos

Exemplos:

- fix separado de refactor
- docs separado de feat
- testes separados quando não fizerem parte direta da entrega

Nunca misturar múltiplas intenções no mesmo commit.

---

## 4) Tipos de commit

Escolher o tipo pela intenção:

- feat: nova funcionalidade
- fix: correção de bug
- refactor: mudança interna sem impacto funcional
- test: testes
- docs: documentação
- chore: manutenção sem impacto funcional
- perf: melhoria de performance
- build: build/dependências
- ci: pipeline/automação

---

## 5) Formato da mensagem

Formato obrigatório:

<type>(escopo): <resumo>

Regras:

- idioma: PT-BR
- resumo claro e específico
- até 100 caracteres
- sem ponto final
- evitar termos vagos:
  - ajustes
  - mudanças
  - correções

Exemplos:

- fix(usuario): corrige validação de email duplicado
- feat(pagamento): adiciona suporte a parcelamento
- refactor(api): separa regras de validação em service

---

## 6) Corpo do commit (quando necessário)

Adicionar somente se agregar valor:

- arquivos principais alterados
- decisões importantes
- riscos ou impactos
- instruções de validação

Exemplo:

- ajusta validação no service de usuário
- remove duplicidade no repository
- adiciona teste de regressão

---

## 7) Validação obrigatória antes de commitar

Antes de executar o commit, garantir:

- build sem erro
- testes passando
- sem erros de lint
- sem imports não utilizados

Se qualquer validação falhar:
→ NÃO commitar

---

## 8) Segurança de execução (CRÍTICO)

Mesmo quando o usuário pedir commit:

1. Mostrar previamente:
   - arquivos que serão commitados
   - mensagem sugerida

2. Validar escopo

3. Só então executar commit

Nunca commitar automaticamente sem validação explícita do escopo.

---

## 9) Checklist final

- Apenas arquivos do escopo estão staged
- Nenhum arquivo sensível incluído
- Commit representa um único objetivo
- Mensagem clara e semântica
- Validações passaram (build/test/lint)

---

## 10) Resultado esperado

- Commits pequenos, organizados e rastreáveis
- Histórico limpo e compreensível
- Zero risco de vazamento ou commit indevido

---
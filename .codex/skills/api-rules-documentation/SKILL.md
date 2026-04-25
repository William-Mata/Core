---

name: api-rules-documentation
description: Documentar regras de negócio e contratos da API com rastreabilidade total ao código, garantindo cobertura completa, clareza de consumo e consistência entre backend e frontend.
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# API Rules Documentation

---

## Objetivo

Criar ou atualizar documentação técnica da API garantindo:

1. Cobertura completa das regras implementadas no backend
2. Contrato de consumo detalhado, realista e utilizável
3. Rastreabilidade direta ao código
4. Clareza suficiente para consumo sem necessidade de leitura do código

Usar `references/doc-template.md` como base quando não houver template do time.

Destinos padrões:

* `documentações tecnica/`
* `../Core-Front/documentação tecnica/API/`

---

## Fonte de verdade

A documentação deve ser baseada EXCLUSIVAMENTE em:

* Código fonte
* Testes automatizados

Nunca assumir comportamento sem evidência.

---

## Regras obrigatórias permanentes

### 1) Índice "Resumo" (OBRIGATÓRIO)

Deve conter:

* descrição do módulo/controller
* lista de TODOS endpoints atuais
* endpoints descontinuados (quando identificados)

---

### 2) Cobertura total da controller

Ao atualizar documentação:

* Documentar TODA a controller
* Nunca documentar apenas endpoints alterados

---

### 3) Contrato completo (OBRIGATÓRIO)

Para cada endpoint:

* Request completo (body, params, query)
* Response completo (JSON realista)

Se houver DTO compartilhado:
→ criar seção **Contratos completos**

---

### 4) Exemplos reais (CRÍTICO)

* Exemplos devem refletir:

  * estrutura real
  * nomes reais
  * tipos reais

* Nunca usar:

  * exemplos genéricos
  * campos fictícios

---

### 5) Regras de negócio (OBRIGATÓRIO)

Para cada endpoint, detalhar:

* validações
* condicionais
* limites
* bloqueios

E também o fluxo:

1. Validação
2. Processamento
3. Persistência
4. Retorno

---

### 6) Efeitos colaterais (OBRIGATÓRIO)

Descrever explicitamente:

* alterações em outras entidades
* logs
* eventos
* integrações externas

---

### 7) Autorização (OBRIGATÓRIO)

Informar claramente:

* quem pode acessar
* regras de permissão
* dependência de usuário/autenticação

---

### 8) Enums e valores possíveis

Para campos relevantes, documentar:

* valores possíveis
* significado de cada valor

---

### 9) Erros (OBRIGATÓRIO)

Nunca usar linguagem vaga.

Para cada erro:

* status HTTP
* condição de disparo
* mensagem/código (quando possível)

---

### 10) Rastreabilidade (OBRIGATÓRIO)

Sempre citar:

* Controller
* Service / UseCase
* DTOs
* Repository
* Enums
* Exceptions
* Testes (quando existirem)

---

### 11) Fato vs Inferência

Separar:

* Fato confirmado
* Inferência

---

### 12) Sincronização Frontend

* Replicar documentação em:

  * `../Core-Front/documentação tecnica/API/`

* Manter:

  * mesmo nome
  * mesmo conteúdo

* Se não for possível:
  → registrar no retorno final

---

## Fluxo de execução

1. Identificar escopo
2. Analisar código completo
3. Mapear regras reais
4. Mapear contrato completo
5. Documentar controller inteira
6. Replicar no frontend

---

## Estrutura recomendada

1. Resumo (índice geral)
2. Contratos completos (DTOs)
3. Endpoints detalhados
4. Matriz de erros
5. Efeitos colaterais
6. Rastreabilidade
7. Fatos vs inferências
8. Pendências

---

## Checklist por endpoint

1. Objetivo
2. Autorização
3. Validações
4. Regras de negócio
5. Fluxo de execução
6. Efeitos colaterais
7. Response completo
8. Erros detalhados
9. Exemplo real

---

## Validação final

Antes de concluir:

* Conferir nomes reais (rotas, DTOs, enums)
* Validar consistência com código
* Validar consistência com frontend
* Validar ausência de lacunas

Se houver dúvidas:
→ registrar em `Pendências`

---

## Formato de entrega

A resposta deve conter:

* Arquivos criados/atualizados
* Resumo das regras documentadas
* Pendências identificadas
* Divergências entre front e back (se houver)

---

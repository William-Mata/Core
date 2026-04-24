---
name: api-rules-documentation
description: Documentar regras de negocio e contratos da API com rastreabilidade ao codigo. Usar quando for necessario mapear validacoes, regras, efeitos colaterais e consumo de endpoints.
---

# API Rules Documentation

## Objetivo

Criar ou atualizar documentacao tecnica da API garantindo:

1. Cobertura completa das regras implementadas no backend.
2. Contrato de consumo detalhado e confiavel.
3. Rastreabilidade direta ao codigo.

Usar [references/doc-template.md](references/doc-template.md) como estrutura base quando nao houver template do time.

Usar a pasta `documentações tecnica/` e `../Core-Front/documentação tecnica/API/` como destino padrao das documentacoes tecnicas desta skill.

---

## Fonte de verdade

A documentacao deve ser baseada EXCLUSIVAMENTE em:

- Codigo fonte
- Testes automatizados (quando existirem)

Nunca assumir comportamento sem evidencia.

---

## Regras obrigatorias permanentes

Estas regras sao obrigatorias em qualquer uso desta skill:

1. O primeiro indice "Resumo" da documentacao deve conter:
- resumo geral do modulo/escopo
- todos os endpoints atuais da controller/rota documentada
- endpoints descontinuados, quando existirem no historico recente

2. Ao receber pedido de "atualizar documentacao", documentar o estado ATUAL da controller inteira.
- Nao documentar apenas endpoints alterados na task.
- Refletir todas as rotas publicas atualmente expostas na controller.

3. Sempre incluir body completo de consumo.
- Request completo para todos endpoints que recebem body.
- Response completa (payload JSON) para todos endpoints relevantes.
- Quando houver DTOs compartilhados, incluir secao de "Contratos completos (request/response)".

4. Separar fatos confirmados de inferencias.
- Fato confirmado: observado diretamente no codigo/testes.
- Inferencia: deducao necessaria por ausencia de evidencia explicita.

5. Garantir rastreabilidade.
- Citar controller, service/use case, DTOs, repositorio, enums/excecoes e testes relacionados.

6. Nao usar linguagem vaga.
- Evitar frases como "pode retornar erro".
- Sempre informar condicao objetiva + codigo/mensagem quando conhecida.

7. Sincronizar backend e frontend.
- Sempre que atualizar documentacao de API no backend, replicar o mesmo arquivo para `../Core-Front/documentação tecnica/API/`.
- Manter o mesmo nome de arquivo entre backend e frontend.
- Se a pasta de destino nao existir, registrar explicitamente no retorno final.

---

## Fluxo de execucao

1. Identificar o escopo pedido pelo usuario:
- modulo inteiro
- endpoint especifico
- regra especifica (exemplo: recorrencia, status, permissao)

2. Localizar fonte primaria no codigo antes de escrever:
- controllers/routers (entrada HTTP)
- services/use cases (regras de negocio)
- validators/schemas
- repositorios e efeitos em persistencia
- enums/exceptions e middlewares de erro

3. Registrar regras reais observadas no codigo:
- validacoes de entrada
- condicionais de negocio
- limites e bloqueios
- side effects (logs, eventos, atualizacoes em outras entidades)
- regras de autorizacao/autenticacao

4. Mapear contrato de consumo:
- metodo e rota
- parametros obrigatorios e opcionais
- payload completo de request
- payload completo de response de sucesso
- codigos de erro e motivo

5. Inserir ou atualizar a documentacao dentro da pasta `documentações tecnica/`.

5.1 Replicar o arquivo atualizado em `../Core-Front/documentação tecnica/API/`.

6. Se o usuario nao informar nome do arquivo, criar nome descritivo no formato:
- `documentações tecnica/<modulo-ou-endpoint>-regras-api.md`

---

## Regras de redacao

Escrever de forma objetiva e auditavel, sempre baseada no codigo atual.

Incluir exemplos minimos de consumo para cada endpoint documentado:
- exemplo de request valido
- exemplo de response de sucesso
- exemplo de erro comum

Quando houver muitos endpoints, usar esta ordem:
1. Indice geral (resumo + todos endpoints)
2. Contratos completos (DTOs request/response)
3. Regras por endpoint
4. Matriz de erros por endpoint
5. Rastreabilidade
6. Fatos confirmados e inferencias
7. Pendencias (se houver)

---

## Checklist minimo por endpoint

Antes de finalizar, garantir que cada endpoint documentado contem:

1. Objetivo da rota
2. Autenticacao/autorizacao exigida
3. Campos obrigatorios e validacoes
4. Regras de negocio aplicadas
5. Efeitos colaterais (persistencia, log, integracoes)
6. Response de sucesso (status + body completo)
7. Responses de erro (status + condicao de disparo + mensagem/codigo quando conhecida)
8. Exemplo de consumo

---

## Validacao

Conferir se a documentacao esta consistente com nomes reais de rotas, DTOs, enums e status code.

Quando houver testes automatizados cobrindo as regras, citar que as regras estao alinhadas aos testes.

Se detectar lacunas (regra no codigo sem documentacao ou comportamento ambiguo), registrar em uma secao `Pendencias` com acao sugerida.

---

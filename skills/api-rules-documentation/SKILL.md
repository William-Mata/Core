---
name: api-rules-documentation
description: Documentar e atualizar regras de negocio da API com foco tecnico e consumivel. Use quando Codex precisar mapear validacoes, restricoes, pre-condicoes, pos-condicoes, efeitos colaterais, erros e contratos de endpoints, e gerar ou atualizar documentacao tecnica explicando quais regras estao aplicadas e como consumir cada rota.
---

# API Rules Documentation

## Objetivo

Criar ou atualizar documentacao tecnica da API com duas frentes obrigatorias:
1. Regras aplicadas no backend (o que a API valida, bloqueia, transforma e persiste).
2. Como consumir (request, response, autenticacao, erros e exemplos).

Usar [references/doc-template.md](references/doc-template.md) como estrutura base sempre que nao houver template do time.

Usar a pasta `documentações tecnica/` como destino padrao das documentacoes tecnicas desta skill.

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
- payload de request
- formato de response de sucesso
- codigos de erro e motivo
5. Inserir ou atualizar a documentacao dentro da pasta `documentações tecnica/`.
6. Se o usuario nao informar nome do arquivo, criar nome descritivo no formato `documentações tecnica/<modulo-ou-endpoint>-regras-api.md`.

## Regras de redacao

Escrever de forma objetiva e auditavel, sempre baseada no codigo atual.

Evitar frases vagas como "pode retornar erro". Especificar quando e por que retorna erro.

Sempre separar fatos confirmados de inferencias:
- fato confirmado: observado diretamente em codigo, teste ou contrato existente
- inferencia: suposicao necessaria por ausencia de evidencia explicita

Incluir exemplos minimos de consumo para cada endpoint documentado:
- exemplo de request valido
- exemplo de response de sucesso
- exemplo de erro comum

## Checklist minimo por endpoint

Antes de finalizar, garantir que cada endpoint documentado contem:
1. Objetivo da rota
2. Autenticacao/autorizacao exigida
3. Campos obrigatorios e validacoes
4. Regras de negocio aplicadas
5. Efeitos colaterais (persistencia, log, integracoes)
6. Respostas de sucesso (status e corpo)
7. Respostas de erro (status, condicao de disparo e mensagem quando conhecida)
8. Exemplo de consumo

## Validacao

Conferir se a documentacao esta consistente com nomes reais de rotas, DTOs, enums e status code.

Quando houver testes automatizados cobrindo as regras, citar que as regras estao alinhadas aos testes.

Se detectar lacunas (regra no codigo sem documentacao ou comportamento ambiguo), registrar em uma secao "Pendencias" com acao sugerida.

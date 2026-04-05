---
name: sdet
description: Atuar como Software Development Engineer in Test em sistemas backend, frontend e APIs. Use quando Codex precisar definir estrategia de testes, identificar lacunas de cobertura, criar ou ajustar testes unitarios, de integracao e ponta a ponta, investigar falhas ou flakiness, revisar risco de regressao, validar cenarios criticos e melhorar a confiabilidade de suites automatizadas.
---

# SDET

Adotar uma postura de engenharia de qualidade. Priorizar risco, comportamento observavel e confiabilidade antes de volume de testes.

## Fluxo

1. Entender o comportamento alvo, o risco de regressao e o nivel correto de teste.
2. Inspecionar os testes existentes antes de criar novos.
3. Preferir o menor teste que prove o comportamento com confiabilidade.
4. Executar validacoes estreitas primeiro e ampliar o escopo depois.
5. Corrigir a causa da falha; nao maquiar o problema com asserts fracos ou skips indevidos.

## Escolha do tipo de teste

- Usar teste unitario quando a regra puder ser validada com dependencias isoladas.
- Usar teste de integracao quando a confiabilidade depender de persistencia, mapeamento, serializacao, DI ou contratos entre camadas.
- Usar teste ponta a ponta apenas quando o risco exigir validar o fluxo completo.
- Evitar duplicar a mesma cobertura em varios niveis sem motivo claro.

## Regras de implementacao

- Nomear testes pelo comportamento esperado.
- Manter cada teste focado em uma regra, fluxo ou falha.
- Preparar dados minimos e explicitos.
- Preferir asserts que validem resultado observavel, nao detalhes acidentais de implementacao.
- Cobrir caminho feliz, bordas relevantes e falhas com impacto real.
- Ao investigar flakiness, buscar dependencia de tempo, ordem, estado compartilhado, concorrencia, I/O e dados nao deterministicos.
- Ao corrigir flakiness, tornar o teste deterministico em vez de apenas aumentar timeout.

## Revisao com mentalidade SDET

Ao revisar mudancas:

- Procurar regressao comportamental antes de estilo.
- Apontar cenarios sem cobertura que possam quebrar em producao.
- Verificar contratos, validacoes, mensagens de erro, ids repetidos, nulabilidade, ordenacao e regras de concorrencia quando fizer sentido.
- Destacar gaps de observabilidade e diagnostico se uma falha seria dificil de reproduzir.

## Validacao

- Rodar primeiro o menor recorte de testes afetado.
- Se o projeto permitir, validar depois a suite mais ampla relacionada ao modulo.
- Se nao for possivel executar testes, registrar exatamente o bloqueio.

## Saida esperada

Entregar conclusoes objetivas:

- risco coberto
- testes criados ou ajustados
- testes executados
- falhas encontradas
- riscos residuais ou lacunas que permaneceram

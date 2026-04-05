---
name: semantic-commit
description: Criar commits Git com semantica convencional e staging seguro, sempre com mensagens em PT-BR. Use quando Codex precisar revisar alteracoes locais, agrupar arquivos relacionados, evitar staging de mudancas nao relacionadas e escrever uma mensagem de commit curta e semantica em portugues.
---

# Semantic Commit

Inspecione o worktree antes de commitar. Use `git status --short` e `git diff --cached --stat` ou `git diff --stat` para entender o escopo.

Faça stage apenas dos arquivos que pertencem a mudanca solicitada. Nao inclua edicoes nao relacionadas, artefatos gerados, anotacoes locais, segredos ou arquivos temporarios.

Prefira um commit logico por assunto. Separe as mudancas quando misturarem correcao, refactor, documentacao ou manutencao sem relacao entre si.

Escolha o tipo do commit pela intencao:

- `feat`: nova funcionalidade ou comportamento visivel
- `fix`: correcao de bug ou regressao
- `refactor`: mudanca interna sem intencao funcional direta
- `test`: criacao ou ajuste de testes
- `docs`: alteracao apenas de documentacao
- `chore`: manutencao, tooling, ignore rules ou organizacao sem impacto funcional
- `perf`: melhoria de desempenho
- `build`: alteracao de build, dependencia ou empacotamento
- `ci`: alteracao de pipeline ou automacao

Use um assunto curto e direto:

- formato: `<type>: <resumo>`
- escreva sempre em PT-BR
- mantenha especifico e, quando possivel, abaixo de 72 caracteres
- nao use ponto final no assunto
- preserve maiusculas apenas quando nomes exigirem

Adicione corpo apenas quando melhorar a rastreabilidade. Use bullets curtos para:

- principais areas ou arquivos alterados
- restricoes ou exclusoes relevantes
- notas de migracao, validacao ou risco

Antes de commitar:

1. Confirme que os arquivos staged pertencem a um unico objetivo.
2. Revise se nenhum arquivo ignorado ou local foi staged por engano.
3. Prefira linguagem semantica e especifica em vez de resumos vagos como `ajustes`, `mudancas` ou `correcoes`.
4. Nao escreva mensagem de commit em ingles.

Bons exemplos:

- `fix: corrige validacao de email duplicado no cadastro de usuario`
- `refactor: reorganiza projeto de testes na raiz do repositorio`
- `docs: sanitiza readme para orientacoes locais`
- `chore: ignora anotacoes operacionais locais`

Se o usuario pedir para commitar, execute o commit em vez de apenas sugerir a mensagem.

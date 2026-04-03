# AGENTS.md

## Contexto do projeto

Leia o `README.md` na raiz antes de qualquer tarefa. Ele é a fonte de verdade sobre estrutura, stack, módulos, configuração e como executar o projeto.

---

## Skills disponíveis

Leia o arquivo do skill correspondente antes de iniciar a tarefa.

| Situação                                                                 | Skill a carregar                                           |
|--------------------------------------------------------------------------|------------------------------------------------------------|
| Implementar, corrigir, revisar ou testar funcionalidades do backend      | `.codex/skills/core-clean-architecture-api.md`             |
| Definir estratégia de testes, criar/ajustar testes, investigar falhas    | `.codex/skills/sdet.md`                                    |
| Analisar queries, índices, performance ou escrever scripts T-SQL         | `.codex/skills/sql-server-dba.md`                          |
| Documentar ou atualizar regras de negócio e contratos de endpoints       | `.codex/skills/api-rules-documentation.md`                 |
| Fazer staging e escrever mensagem de commit                              | `.codex/skills/semantic-commit.md`                         |

Quando a tarefa envolver mais de um skill, leia todos os relevantes antes de começar.

---

## Regras gerais

- Nunca misturar camadas: controllers finos, lógica em Application, domínio livre de infraestrutura.
- Lançar `DomainException` e `NotFoundException` para erros de negócio — o middleware cuida do HTTP.
- Seguir nomenclatura existente: `*Controller`, `*Service`, `*Repository`, `*Request`, `*Dto`, `*Validator`.
- Commits sempre em PT-BR com semântica convencional.
- Documentação técnica salvar em `documentações tecnica/`.
- Scripts SQL sempre com validação prévia via `SELECT` antes de `UPDATE`/`DELETE`.
- Nunca versionar `appsettings.Development.json`, secrets, tokens ou connection strings reais.

---

## Validação padrão

Antes de encerrar qualquer tarefa que altere código:

1. Rodar testes do módulo afetado:
   ```bash
   dotnet test .\Core.Tests\Core.Tests.csproj --filter [NomeDoTestClass]
   ```
2. Rodar suite completa ao tocar contratos compartilhados, `AppDbContext`, middlewares ou autenticação:
   ```bash
   dotnet test .\Core.Tests\Core.Tests.csproj
   ```
3. Confirmar que nenhuma camada foi violada.
4. Verificar se documentação técnica precisa ser atualizada.
5. Fazer commit semântico em PT-BR com apenas os arquivos da mudança.

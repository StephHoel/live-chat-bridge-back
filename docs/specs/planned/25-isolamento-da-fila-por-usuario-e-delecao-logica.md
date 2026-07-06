# Mini-spec: Isolamento da fila por usuário e deleção lógica

Número: 25
Status: planejado
Origem: desdobramento técnico posterior da Spec 07 e alinhamento com a diretriz de isolamento por usuário/sessão

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- A fila atual é persistida sem contexto do usuário dono da sessão ativa.
- O endpoint `GET /queue` foi implementado inicialmente sobre o modelo global existente e hoje não isola a fila por usuário autenticado.
- A remoção atual de itens da fila não possui deleção lógica, o que dificulta histórico operacional, reentrada controlada e queries consistentes de itens ativos.

## Objetivo

- Evoluir o domínio de fila para que cada sessão lógica de usuário possua sua própria fila.
- Adicionar deleção lógica para itens de fila por meio de campo temporal controlado pelo backend.
- Tornar consultas, comandos e atualizações de fila compatíveis com o isolamento por usuário/sessão já adotado no worker.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/done/07-endpoint-http-fila-bootstrap.md](../done/07-endpoint-http-fila-bootstrap.md): redefine a semântica da leitura de fila para retornar apenas itens ativos do usuário autenticado, substituindo a leitura global atualmente transitória.
- Interfere com [docs/specs/done/03-persistencia-duravel-repositorios.md](../done/03-persistencia-duravel-repositorios.md): altera a identidade persistida da fila, seus índices e as consultas esperadas do repositório.
- Interfere com [docs/specs/done/05-processamento-real-chat-worker.md](../done/05-processamento-real-chat-worker.md): a atualização de fila no processamento assíncrono passa a depender do contexto do dono da sessão e de regras de reativação lógica.
- Interfere com [docs/specs/done/18-acionamento-do-worker-pelo-front.md](../done/18-acionamento-do-worker-pelo-front.md): deve reutilizar o isolamento por usuário/sessão ativa já definido para o ciclo operacional do worker.
- Interfere com [docs/specs/done/19-configuracao-persistida-de-live-e-usernames.md](../done/19-configuracao-persistida-de-live-e-usernames.md): a configuração operacional por usuário continua sendo a base do contexto dono da sessão onde a fila será mantida.
- Interfere com [docs/specs/planned/10-comandos-iniciais-e-dispatcher.md](10-comandos-iniciais-e-dispatcher.md): o comando `!fila` deve consultar posição e tempo de entrada apenas na fila ativa do contexto correto.

## Comportamento esperado

- Cada item de fila deve pertencer explicitamente a um usuário dono da sessão ativa.
- Consultas de fila devem retornar apenas itens ativos (`DeletedAt == null`) do usuário autenticado.
- Entradas repetidas do mesmo participante no mesmo contexto devem reativar ou atualizar o item lógico existente, conforme regra definida pelo backend.
- Remoções de fila devem marcar deleção lógica, sem apagar o histórico de forma física nesta fase.

## Superfícies afetadas

- Endpoints: `GET /queue` e quaisquer endpoints futuros de mutação/consulta operacional de fila.
- Handlers: leitura da fila e processamento de ingestão que cria/atualiza entradas de fila.
- Workers/Provedores: fluxo assíncrono que encaminha mensagens elegíveis para entrada na fila.
- Repositórios/Persistência: `IQueueRepository`, implementação concreta, configuração EF Core e migrations.
- Integrações externas: sem alteração obrigatória de protocolo.

## Dados e persistência

- Evoluir `QueueEntity` com contexto mínimo de dono da fila, preferencialmente `OwnerUserId`.
- Adicionar `DeletedAt` (`DateTime?`) para deleção lógica.
- Redefinir índices e unicidade da fila para abandonar o modelo global por `User`.
- Regra recomendada de unicidade funcional: participante ativo único por dono da sessão, considerando deleção lógica.
- Consultas padrão do repositório devem excluir registros logicamente deletados por default, salvo operação administrativa explícita.
- A solução deve permanecer compatível com SQLite atual e com a futura migração para PostgreSQL.

## Contratos de API

- Esta mini-spec não cria obrigatoriamente endpoint novo por si só.
- O contrato de `GET /queue` deve passar a refletir apenas a fila ativa do usuário autenticado.
- Endpoints futuros de mutação devem seguir envelope `Result<T>` já adotado pelo projeto.

## Regras de validação

- O dono da fila deve ser derivado de contexto autenticado confiável, nunca do cliente externo.
- Itens logicamente deletados não devem aparecer em leituras operacionais normais.
- Uma nova entrada do mesmo participante no mesmo contexto não deve criar duplicidade ativa.
- A deleção lógica não pode quebrar a rastreabilidade temporal de entrada original quando a regra escolhida exigir preservação.

## Critérios de aceite

- Fila passa a ser isolada por usuário/sessão ativa.
- `GET /queue` retorna apenas itens ativos do usuário autenticado.
- O domínio suporta deleção lógica sem exclusão física obrigatória.
- Reentrada de participante no mesmo contexto não gera duplicidade ativa inconsistente.
- Fluxos HTTP e worker mantêm comportamento coerente no mesmo contexto de fila.

## Testes esperados

- Testes de repositório para consultas filtrando apenas itens ativos por dono da fila.
- Testes de handler para leitura da fila do usuário autenticado sem vazamento entre usuários.
- Testes de ingestão para criação, reativação e não duplicação de item ativo.
- Testes de integração para `GET /queue` com isolamento entre usuários autenticados distintos.
- Testes de migration cobrindo novo schema e retrocompatibilidade dos dados existentes.

## Fora de escopo

- Canal SSE.
- Regras avançadas de ordenação além do contrato já definido em `GET /queue`.
- Dashboard administrativo de histórico de remoções.
- Estratégia de retenção/expurgo de itens logicamente deletados.

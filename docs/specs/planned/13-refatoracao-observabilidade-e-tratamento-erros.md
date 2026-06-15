# Mini-spec: Refatoração transversal de observabilidade e tratamento de erros

Número: 13
Status: planejado

## Problema

- Há duplicação de blocos `try/catch/finally` com logs em múltiplos componentes (`Application`, `Infrastructure` e `Worker`).
- O tratamento de falhas e o padrão de logs não são uniformes entre handlers, repositórios e providers.
- Existem saídas diretas em console em pontos de infraestrutura, dificultando observabilidade consistente e rastreabilidade por correlação.

## Comportamento esperado

- Centralizar o padrão de execução com logging, erro e encerramento em um comportamento reutilizável.
- Padronizar logs estruturados para início, sucesso, erro e fim de operação com correlação.
- Remover `Console.WriteLine` de componentes de integração, delegando observabilidade ao logger.
- Manter o comportamento funcional atual dos casos de uso (sem mudança de contrato público).

## Superfícies afetadas

- Endpoints: sem mudança de rota; impacto indireto na padronização de logs de requisição.
- Handlers: `LoginHandler`, `MessageIngestHandler` e futuros handlers de comando/caso de uso.
- Workers/Provedores: `ChatWorker`, `TikTokChatProvider`.
- Integrações externas: sem alteração de protocolo; apenas padronização de telemetria/log.

## Dados e persistência

- Sem novos dados de domínio obrigatórios.
- Sem mudança de schema de persistência.
- Garantir que mensagens de erro não exponham dados sensíveis (token, senha, payload bruto sensível).

## Contratos de API

- Request: sem alterações.
- Response: sem alterações de estrutura.
- Códigos HTTP: sem alteração de semântica dos fluxos existentes.

## Regras de validação

- Operações com erro esperado de negócio devem continuar retornando `Result<T>.Fail(...)` sem exceção.
- Exceções inesperadas devem ser registradas em log estruturado e convertidas para erro interno quando aplicável.
- Logs devem incluir contexto mínimo: nome da operação, `correlationId` (quando disponível), tempo de execução e status final.

## Critérios de aceite

- Redução mensurável de duplicação de `try/catch/finally` nos handlers e serviços-alvo.
- Logs de início/fim/erro padronizados nas superfícies afetadas.
- Nenhum `Console.WriteLine` remanescente em providers de integração.
- Comportamento funcional das rotas e worker permanece equivalente ao estado atual.

## Testes esperados

- Testes unitários validando comportamento do executor/decorator de operação em sucesso e falha.
- Testes unitários garantindo que erros esperados não são transformados em exceções.
- Testes de regressão para `LoginHandler` e `MessageIngestHandler` com foco em resultado funcional.

## Fora de escopo

- Alterar regras de negócio de autenticação, idempotência, fila ou comandos.
- Introduzir stack externa de observabilidade (APM/Tracing distribuído) nesta fase.
- Redesenhar contratos HTTP ou payloads de integração externa.

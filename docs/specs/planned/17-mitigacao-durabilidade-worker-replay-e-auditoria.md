# Mini-spec: Mitigacao de durabilidade do worker com replay e auditoria

Numero: 17
Status: planejado
Origem: risco registrado na PR #9 (semantica at-least-once intra-processo sem garantia cross-restart)

## Problema

- O fluxo assincrono do worker usa canal em memoria e nao garante retencao entre reinicios do processo.
- Quedas durante processamento podem causar perda de mensagens que ainda nao foram persistidas de forma recuperavel.
- Falta trilha persistida unificada para auditoria operacional do processamento do worker.
- A tabela `ChatMessages` ainda nao diferencia claramente o ator que inseriu/processou a mensagem no backend.

## Comportamento esperado

- Implementar buffer duravel de entrada para o worker (store-then-process) antes do processamento de negocio.
- Permitir replay pos-restart de mensagens pendentes/falhas com estrategia de retentativa controlada.
- Manter reutilizacao do caso de uso de ingestao para preservar regras de idempotencia, fila e comandos.
- Persistir auditoria minima do fluxo com ator, acao, status e timestamp em tabela dedicada.
- Preencher origem de insercao da mensagem em `ChatMessages` para distinguir autor do chat e ator de insercao no sistema.

## Interferencia com mini-specs existentes

- Interfere com [docs/specs/done/05-processamento-real-chat-worker.md](docs/specs/done/05-processamento-real-chat-worker.md): expande a semantica de entrega do worker para garantir recuperacao cross-restart.
- Interfere com [docs/specs/planned/15-tabela-logs-com-auditoria-minima.md](docs/specs/planned/15-tabela-logs-com-auditoria-minima.md): incorpora a trilha de auditoria minima no mesmo fluxo de mitigacao.
- Interfere com [docs/specs/planned/16-campo-auditoria-origem-insercao-chatmessages.md](docs/specs/planned/16-campo-auditoria-origem-insercao-chatmessages.md): incorpora o preenchimento de origem de insercao em todas as entradas.

### Decisao explicita do usuario

- Implementar esta mitigacao em escopo consolidado, incluindo auditoria minima e origem de insercao.

## Superficies afetadas

- Endpoints: sem mudanca obrigatoria de contrato publico nesta fase.
- Handlers: `MessageIngestHandler` permanece como regra central de negocio.
- Workers/Provedores: `ChatWorker`, `ChatProcessorService`, `TikTokChatProvider` (ou provedores equivalentes).
- Repositorios/Persistencia: novo repositorio de inbox do worker, auditoria e evolucao de `ChatMessages`.
- Integracoes externas: sem alteracao de protocolo com provider nesta fase.

## Dados e persistencia

### 1) Inbox duravel do worker

Criar tabela de entrada duravel para mensagens recebidas do provider (ex.: `WorkerInboxMessages`), com campos minimos:

- `Id` (Guid)
- `Provider` (string)
- `Author` (string)
- `Text` (string)
- `Timestamp` (DateTime)
- `IdempotencyKey` (string)
- `Status` (string/enum): `Pending`, `Processing`, `Processed`, `Failed`, `DeadLetter`
- `Attempts` (int)
- `NextRetryAtUtc` (DateTime?)
- `LastError` (string?)
- `CreatedAtUtc` (DateTime)
- `UpdatedAtUtc` (DateTime)

Indices recomendados:

- `IdempotencyKey` (unico quando aplicavel ao inbox)
- `Status + NextRetryAtUtc`
- `CreatedAtUtc`

### 2) Auditoria minima

Criar/usar tabela `AuditLogs` com campos minimos:

- `Id` (Guid)
- `CreatedAtUtc` (DateTime)
- `ActorUser` (string)

Campos recomendados:

- `Action` (string)
- `Resource` (string)
- `Status` (string)
- `MetadataJson` (string?)

### 3) Origem de insercao em ChatMessages

Adicionar coluna `InsertedByUser` em `ChatMessages`:

- Obrigatoria apos migracao.
- Fluxo HTTP: usuario autenticado do token.
- Fluxo worker: identificador tecnico controlado, ex.: `system:worker`.
- Dados legados: preencher com fallback seguro em migracao.

## Fluxo funcional proposto

1. Provedor recebe mensagem e persiste em `WorkerInboxMessages` com `Status=Pending`.
2. Processador faz claim atomico de lote `Pending`/`Failed` elegivel (`NextRetryAtUtc <= now`) para `Processing`.
3. Processador executa `MessageIngestHandler` com os dados da inbox.
4. Em sucesso:
   - marca inbox como `Processed`;
   - persiste auditoria de sucesso;
   - garante `InsertedByUser` em `ChatMessages`.
5. Em falha transitória:
   - incrementa `Attempts`;
   - grava `LastError`;
   - calcula `NextRetryAtUtc` (backoff);
   - retorna para `Failed`.
6. Ao exceder limite de tentativas:
   - marca `DeadLetter`;
   - persiste auditoria de falha final.
7. No startup/restart:
   - rotina de recuperacao varre `Pending` e `Failed` elegiveis e retoma processamento.

## Semantica de entrega

- Objetivo: at-least-once com recuperacao cross-restart.
- Efeitos duplicados continuam prevenidos por idempotencia ja existente no fluxo de ingestao.
- A garantia de exactly-once nao faz parte desta mini-spec.

## Contratos de API

- Request/Response HTTP: sem alteracao obrigatoria nesta etapa.
- Códigos HTTP: sem impacto direto esperado.
- Envelope `Result<T>` permanece inalterado em endpoints.

## Regras de validacao

- Mensagens invalidas devem ser auditadas e descartadas com motivo estruturado.
- `InsertedByUser` nao pode ser nulo/vazio para novas mensagens.
- `ActorUser` nao pode expor segredo (senha/token/chave).
- Claim de processamento deve ser atomico para evitar corrida entre workers futuros.

## Criterios de aceite

- Reiniciar o processo nao perde mensagens ja recebidas e persistidas na inbox.
- Mensagens pendentes/falhas elegiveis sao reprocessadas apos restart.
- Duplicatas nao geram efeito colateral adicional no dominio.
- Entradas de auditoria sao persistidas com `CreatedAtUtc` e `ActorUser`.
- `ChatMessages.InsertedByUser` e preenchido corretamente em fluxo HTTP e worker.
- Mensagens com falha recorrente chegam em `DeadLetter` apos limite de tentativas.

## Testes esperados

- Testes de repositorio da inbox duravel: create, claim, transicoes de status e consultas por elegibilidade.
- Testes de processamento: sucesso, falha transitoria com retry, falha final com dead-letter.
- Testes de restart: mensagens `Pending/Failed` sao retomadas no boot.
- Testes de auditoria: persistencia de `CreatedAtUtc` e `ActorUser` sem dados sensiveis.
- Testes de `InsertedByUser`:
  - HTTP autenticado preenche ator do token.
  - Worker preenche `system:worker`.
  - Migracao preenche legado com fallback.

## Fora de escopo

- Troca para broker externo distribuido (RabbitMQ/Kafka/SQS/Service Bus).
- Dashboard de monitoramento de DLQ/auditoria.
- Exactly-once distribuido.

## Plano sugerido de implementacao (incremental)

1. Migracoes de schema: `WorkerInboxMessages`, `AuditLogs`, `ChatMessages.InsertedByUser`.
2. Repositorios e servicos de claim/retry/recovery.
3. Integracao do provider com persistencia de inbox (store-then-process).
4. Integracao do processador com transicoes de status + auditoria + dead-letter.
5. Testes de regressao e cenarios de restart.

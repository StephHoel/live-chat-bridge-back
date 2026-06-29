# Mini-spec: Mitigação de durabilidade do worker com replay e auditoria

Número: 17
Status: planejado
Origem: risco registrado na PR #9 (semântica at-least-once intra-processo sem garantia cross-restart)

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- O fluxo assíncrono do worker usa canal em memória e não garante retenção entre reinícios do processo.
- Quedas durante processamento podem causar perda de mensagens que ainda não foram persistidas de forma recuperável.
- Falta trilha persistida unificada para auditoria operacional do processamento do worker.
- A tabela `ChatMessages` ainda não diferencia claramente o ator que inseriu/processou a mensagem no backend.

## Comportamento esperado

- Implementar buffer durável de entrada para o worker (store-then-process) antes do processamento de negócio.
- Permitir replay pós-restart de mensagens pendentes/falhas com estratégia de retentativa controlada.
- Manter reutilização do caso de uso de ingestão para preservar regras de idempotência, fila e comandos.
- Persistir auditoria mínima do fluxo com ator, ação, status e timestamp em tabela dedicada.
- Preencher origem de inserção da mensagem em `ChatMessages` para distinguir autor do chat e ator de inserção no sistema.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/done/05-processamento-real-chat-worker.md](docs/specs/done/05-processamento-real-chat-worker.md): expande a semântica de entrega do worker para garantir recuperação cross-restart.
- Interfere com [docs/specs/planned/15-tabela-logs-com-auditoria-minima.md](docs/specs/planned/15-tabela-logs-com-auditoria-minima.md): incorpora a trilha de auditoria mínima no mesmo fluxo de mitigação.
- Interfere com [docs/specs/done/16-campo-auditoria-origem-insercao-chatmessages.md](docs/specs/done/16-campo-auditoria-origem-insercao-chatmessages.md): reutiliza e amplia o preenchimento de origem de inserção em todas as entradas.

### Decisão explícita do usuário

- Implementar esta mitigação em escopo consolidado, incluindo auditoria mínima e origem de inserção.

## Superfícies afetadas

- Endpoints: sem mudança obrigatória de contrato público nesta fase.
- Handlers: `MessageIngestHandler` permanece como regra central de negócio.
- Workers/Provedores: `ChatWorker`, `ChatProcessorService`, `TikTokChatProvider` (ou provedores equivalentes).
- Repositórios/Persistência: novo repositório de inbox do worker, auditoria e evolução de `ChatMessages`.
- Integrações externas: sem alteração de protocolo com provider nesta fase.

## Dados e persistência

### 1) Inbox durável do worker

Criar tabela de entrada durável para mensagens recebidas do provider (ex.: `WorkerInboxMessages`), com campos mínimos:

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

Índices recomendados:

- `IdempotencyKey` (único quando aplicável ao inbox)
- `Status + NextRetryAtUtc`
- `CreatedAtUtc`

### 2) Auditoria mínima

Criar/usar tabela `AuditLogs` com campos mínimos:

- `Id` (Guid)
- `CreatedAtUtc` (DateTime)
- `ActorUser` (string)

Campos recomendados:

- `Action` (string)
- `Resource` (string)
- `Status` (string)
- `MetadataJson` (string?)

### 3) Origem de inserção em ChatMessages

Adicionar coluna `InsertedByUser` em `ChatMessages`:

- Obrigatória após migração.
- Fluxo HTTP: usuário autenticado do token.
- Fluxo worker: usuário autenticado que ativou a sessão do worker.
- Dados legados: preencher com fallback seguro em migração.

## Fluxo funcional proposto

1. Provedor recebe mensagem e persiste em `WorkerInboxMessages` com `Status=Pending`.
2. Processador faz claim atômico de lote `Pending`/`Failed` elegível (`NextRetryAtUtc <= now`) para `Processing`.
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
   - rotina de recuperação varre `Pending` e `Failed` elegíveis e retoma processamento.

## Semântica de entrega

- Objetivo: at-least-once com recuperação cross-restart.
- Efeitos duplicados continuam prevenidos por idempotência já existente no fluxo de ingestão.
- A garantia de exactly-once não faz parte desta mini-spec.

## Contratos de API

- Request/Response HTTP: sem alteração obrigatória nesta etapa.
- Códigos HTTP: sem impacto direto esperado.
- Envelope `Result<T>` permanece inalterado em endpoints.

## Regras de validação

- Mensagens inválidas devem ser auditadas e descartadas com motivo estruturado.
- `InsertedByUser` não pode ser nulo/vazio para novas mensagens.
- `ActorUser` não pode expor segredo (senha/token/chave).
- Claim de processamento deve ser atômico para evitar corrida entre workers futuros.

## Critérios de aceite

- Reiniciar o processo não perde mensagens já recebidas e persistidas na inbox.
- Mensagens pendentes/falhas elegíveis são reprocessadas após restart.
- Duplicatas não geram efeito colateral adicional no domínio.
- Entradas de auditoria são persistidas com `CreatedAtUtc` e `ActorUser`.
- `ChatMessages.InsertedByUser` é preenchido corretamente em fluxo HTTP e worker.
- Mensagens com falha recorrente chegam em `DeadLetter` após limite de tentativas.

## Testes esperados

- Testes de repositório da inbox durável: create, claim, transições de status e consultas por elegibilidade.
- Testes de processamento: sucesso, falha transitória com retry, falha final com dead-letter.
- Testes de restart: mensagens `Pending/Failed` são retomadas no boot.
- Testes de auditoria: persistência de `CreatedAtUtc` e `ActorUser` sem dados sensíveis.
- Testes de `InsertedByUser`:
  - HTTP autenticado preenche ator do token.
  - Worker preenche usuário autenticado que ativou a sessão.
  - Migração preenche legado com fallback.

## Fora de escopo

- Troca para broker externo distribuído (RabbitMQ/Kafka/SQS/Service Bus).
- Dashboard de monitoramento de DLQ/auditoria.
- Exactly-once distribuído.

## Plano sugerido de implementação (incremental)

1. Migrações de schema: `WorkerInboxMessages`, `AuditLogs`, `ChatMessages.InsertedByUser`.
2. Repositórios e serviços de claim/retry/recovery.
3. Integração do provider com persistência de inbox (store-then-process).
4. Integração do processador com transições de status + auditoria + dead-letter.
5. Testes de regressão e cenários de restart.

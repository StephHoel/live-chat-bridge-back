# Mini-spec: Rollout de auditoria operacional no projeto

Número: 23
Status: planejado

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- A trilha de auditoria operacional ainda não está aplicada de forma consistente ao longo dos fluxos do sistema.
- A Spec 15 define a base de persistência da auditoria, mas o projeto ainda precisa de um plano faseado para adoção progressiva em handlers, serviços e workers.
- Sem um rollout explícito, há risco de implementações ad-hoc, inconsistências de payload e retrabalho entre as specs de auditoria.

## Decisões consolidadas desta spec

- A Spec 15 deve implementar apenas a base de auditoria: tabela e forma de salvar (`service -> repository`), sem alterar serviços já implementados.
- O campo `Status` da auditoria deve ser modelado como enum no domínio e persistido como string no banco.
- `MetadataJson` deve suportar payload operacional mais rico, com estrutura versionável.
- A implementação de auditoria operacional no processamento assíncrono (worker/replay/retry/dead-letter) pertence exclusivamente a esta spec.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/done/15-tabela-logs-com-auditoria-minima.md](../done/15-tabela-logs-com-auditoria-minima.md): esta spec detalha a estratégia de rollout e restringe o escopo inicial da 15 para infraestrutura básica sem tocar serviços existentes.
- Interfere com [docs/specs/planned/17-mitigacao-durabilidade-worker-replay-e-auditoria.md](../planned/17-mitigacao-durabilidade-worker-replay-e-auditoria.md): a 17 deve reutilizar a infraestrutura de auditoria criada na 15, sem redefinir contratos centrais de `AuditLog`.
- Interfere com [docs/specs/planned/21-nome-de-usuario-para-auditoria-operacional.md](../planned/21-nome-de-usuario-para-auditoria-operacional.md): a evolução do ator de auditoria (email -> nome de exibição) deve ser compatível com o contrato de `ActorUser` definido na 15.
- Complementa [docs/specs/done/13-refatoracao-observabilidade-e-tratamento-erros.md](../done/13-refatoracao-observabilidade-e-tratamento-erros.md): logs estruturados em runtime permanecem, enquanto esta spec cobre trilha persistida de auditoria.

## Objetivo

- Definir uma estratégia incremental para aplicar auditoria persistida ao longo do projeto, minimizando risco de regressão funcional.
- Padronizar contrato de auditoria (`ActorUser`, `Action`, `Resource`, `Status`, `MetadataJson`, `CreatedAtUtc`) para novos pontos de instrumentação.
- Estabelecer governança para evolução futura de eventos auditáveis sem quebrar compatibilidade.

## Escopo por fase

### Fase 1 - Fundação (Spec 15)

- Criar entidade e tabela `AuditLogs`.
- Criar enum de status de auditoria com persistência como string.
- Criar `AuditLogService` para orquestrar escrita e sanitização básica de payload.
- Criar repositório de auditoria e registrar em DI.
- Não alterar serviços/handlers/workers já implementados.

### Fase 2 - Adoção em fluxos operacionais prioritários

- Definir e congelar o catálogo inicial fechado de `Action`/`Resource` como pré-requisito da instrumentação da fase, evitando retrabalho de normalização.
- Integrar auditoria em fluxos de controle operacional (início/parada/status de worker, configuração de live, operações administrativas).
- Aplicar via `AuditLogService` sem acoplamento direto de handlers ao repositório.
- Garantir que a adoção não altere contratos HTTP públicos.
- Implementar política mínima obrigatória de retenção e manutenção de `AuditLogs` antes do encerramento da fase 2.

### Fase 3 - Cobertura ampliada e padronização

- Implementar auditoria operacional no processamento assíncrono, incluindo eventos de worker/replay/retry/dead-letter, sem duplicar implementação na Spec 17.
- Expandir para os demais fluxos de ingestão do projeto como requisito obrigatório desta fase.
- Evoluir o catálogo de `Action`/`Resource` já fechado na fase 2, mantendo compatibilidade retroativa.
- Adicionar telemetria de qualidade da auditoria (campos faltantes, volume por fluxo, latência de escrita).

## Gate de qualidade do rollout

- Decisão: todos os fluxos desta mini-spec são obrigatórios por fase, com exceção do fluxo de durabilidade/replay de inbox pertencente à Spec 17.

### Fluxos obrigatórios por fase

- Fase 1:
  - infraestrutura de auditoria (`AuditLogs`, enum de status, `AuditLogService`, repositório e DI).

- Fase 2:
  - endpoints operacionais (`POST /worker/start`, `POST /worker/stop`, `GET /worker/status`, `GET /config/live`, `PUT /config/live`);
  - tarefas de sistema auditáveis;
  - política de retenção/manutenção ativa.

- Fase 3:
  - fluxo assíncrono auditável sob responsabilidade desta spec (`WorkerFlow`: início, sucesso, falha com retry, dead-letter, retomada pós-restart);
  - demais fluxos de ingestão do projeto dentro do escopo da Spec 23.

### Exclusão explícita

- Não faz parte deste gate o fluxo de durabilidade/replay técnico da Spec 17 (persistência/claim/transição funcional da inbox sem escrita de auditoria).

### Critérios mensuráveis de aceite do gate

- Cobertura de fluxos obrigatórios por fase: 100% dos fluxos listados acima instrumentados e validados em teste de integração.
- Conformidade de catálogo: 100% dos eventos usando valores de `Action`/`Resource` permitidos para a fase vigente.
- Conformidade de metadata: 100% dos eventos com `MetadataJson` válido no contrato obrigatório (`metadataVersion=1`, campos obrigatórios por categoria e tamanho <= 4 KB).
- Política de falha da auditoria: 100% dos fluxos obrigatórios validando segunda tentativa e fallback em log estruturado na dupla falha.
- Não regressão de contrato HTTP: 100% dos testes de contrato de API dos endpoints operacionais aprovados sem mudança de envelope `Result<T>`.

## Catálogo inicial fechado de `Action` e `Resource`

- Objetivo: evitar retrabalho de normalização semântica no rollout de auditoria.
- Regra de uso: durante a fase 2, novos eventos auditáveis devem usar apenas valores deste catálogo inicial fechado.
- Evolução: inclusão de novos valores fica permitida a partir da fase 3, com revisão explícita de compatibilidade.

### `Resource` iniciais aprovados

- `WorkerControl`
- `LiveSettings`
- `OperationalAdmin`
- `WorkerInbox`
- `WorkerReplay`
- `WorkerDeadLetter`
- `SystemTask`

### `Action` iniciais aprovadas

- `WorkerStartRequested`
- `WorkerStartSucceeded`
- `WorkerStartFailed`
- `WorkerStopRequested`
- `WorkerStopSucceeded`
- `WorkerStopFailed`
- `WorkerStatusChecked`
- `LiveSettingsViewed`
- `LiveSettingsUpdated`
- `LiveSettingsUpdateFailed`
- `OperationalActionRequested`
- `OperationalActionSucceeded`
- `OperationalActionFailed`
- `WorkerInboxProcessingStarted`
- `WorkerInboxProcessingSucceeded`
- `WorkerInboxProcessingFailed`
- `WorkerRetryScheduled`
- `WorkerDeadLetterMoved`
- `WorkerPendingRecoveryStarted`
- `WorkerPendingRecoveryFinished`
- `SystemTaskStarted`
- `SystemTaskSucceeded`
- `SystemTaskFailed`

## Delimitação de escopo com a Spec 17

- A Spec 17 implementa durabilidade/replay do worker e evolução de `ChatMessages.InsertedByUser`, sem escrita de auditoria em `AuditLogs`.
- Esta spec concentra toda a instrumentação de auditoria operacional do fluxo assíncrono.
- Eventos mínimos de auditoria assíncrona nesta spec:
  - início de processamento do item de inbox;
  - sucesso de processamento;
  - falha transitória com retry agendado;
  - falha final com envio para dead-letter;
  - retomada pós-restart de itens pendentes/falhos elegíveis.

## Contrato canônico de `ActorUser` (estado atual)

- Valor persistido atual: manter o identificador já utilizado hoje no projeto (e-mail do usuário autenticado quando houver).
- Fallback atual: quando não houver usuário autenticado, usar identificador técnico controlado (ex.: `system:worker`).
- Regra de renomeação: nesta spec não há renomeação de valores já persistidos em `ActorUser`.
- Política de migração histórica: nesta spec não há migração retroativa de histórico de `ActorUser`.
- Evolução planejada: a mudança de ator (e-mail -> nome de exibição) será tratada pela Spec 21, preservando compatibilidade com a base existente.

## Política de falha da escrita de auditoria por fluxo

- Regra geral para todos os fluxos auditáveis:
  - ao falhar a escrita em `AuditLogs`, tentar uma segunda escrita imediata;
  - se a segunda tentativa falhar, registrar erro estruturado em log para recuperação futura.

- Endpoints operacionais (ex.: `POST /worker/start`, `POST /worker/stop`, `GET /worker/status`, `GET/PUT /config/live`):
  - aplicar a regra geral de duas tentativas;
  - a falha de auditoria não deve alterar contrato HTTP público nem status final da operação de negócio.

- Worker/retry/dead-letter:
  - aplicar a regra geral de duas tentativas para cada evento auditável assíncrono;
  - se a auditoria falhar nas duas tentativas, manter a transição de estado funcional do item (`Processed`, `Failed`, `DeadLetter`) e registrar log estruturado para recuperação futura.

- Tarefas de sistema:
  - aplicar a regra geral de duas tentativas;
  - após segunda falha, registrar log estruturado com contexto mínimo (`action`, `resource`, `correlationId` quando houver, motivo da falha) para suporte à recuperação futura.

## Superfícies afetadas

- Endpoints: sem mudança obrigatória de contrato nesta spec.
- Handlers: adoção faseada posterior via chamada a `AuditLogService`.
- Workers/Provedores: adoção faseada para eventos operacionais relevantes.
- Repositórios/Persistência: nova infraestrutura de `AuditLogs`.

## Dados e persistência

- Tabela `AuditLogs` com colunas base:
  - `Id` (Guid)
  - `CreatedAtUtc` (DateTime)
  - `ActorUser` (string)
  - `Action` (string)
  - `Resource` (string)
  - `Status` (string derivada de enum)
  - `MetadataJson` (string opcional)
- Índices mínimos:
  - `CreatedAtUtc`
  - `ActorUser`
  - recomendável composto `Action + CreatedAtUtc`
- Compatibilidade com SQLite atual e migração futura para PostgreSQL.

## Contrato de domínio para status

- Criar enum dedicado (exemplo): `AuditLogStatusEnum`.
- Valores iniciais sugeridos:
  - `Success`
  - `Failure`
  - `Warning`
  - `Info`
- Persistir com `HasConversion<string>()` no EF Core para manter legibilidade e facilitar evolução.

## Política de MetadataJson

- Contrato mínimo obrigatório (Opção B) com `metadataVersion=1`.
- Todo `MetadataJson` deve ser objeto JSON válido e tipado.

### Campos obrigatórios em todos os eventos

- `metadataVersion` (int): valor inicial obrigatório `1`.
- `correlationId` (string): identificador de correlação do fluxo.
- `eventCategory` (string): categoria do evento auditável (`EndpointOperational`, `WorkerFlow`, `SystemTask`).
- `occurredAtUtc` (string ISO 8601): data/hora UTC do evento de auditoria.

### Campos obrigatórios por tipo de evento

- Endpoints operacionais (`eventCategory=EndpointOperational`):
  - `endpointName` (string)
  - `requestPath` (string)
  - `httpStatus` (int)

- Worker/retry/dead-letter (`eventCategory=WorkerFlow`):
  - `provider` (string)
  - `attempt` (int)
  - `workerState` (string)
  - `inboxMessageId` (string guid)

- Tarefas de sistema (`eventCategory=SystemTask`):
  - `taskName` (string)
  - `executionId` (string)
  - `outcome` (string)

### Limite de tamanho

- Tamanho máximo de `MetadataJson`: 4 KB (4096 bytes).
- Payload acima do limite deve ser rejeitado na validação de auditoria, com erro estruturado em log para recuperação futura.

### Sanitização e validação semântica

- Aplicar bloqueio por denylist de chaves sensíveis (`token`, `password`, `secret`, `authorization`, `api_key`, `apikey`, `jwt`).
- Aplicar allowlist por `eventCategory` para os campos obrigatórios e opcionais permitidos no evento.
- Rejeitar payload com campos obrigatórios ausentes, tipos inválidos ou estrutura incompatível com `metadataVersion=1`.

### Estratégia de indexação

- Não indexar JSON bruto de `MetadataJson` nesta fase.
- Manter consultas operacionais por colunas canônicas (`Action`, `Resource`, `CreatedAtUtc`, `ActorUser`).
- Quando necessário por evidência de uso, criar coluna derivada para chave crítica (ex.: `CorrelationId`) em vez de índice direto no JSON.

## Política de retenção e manutenção

- A política de retenção/manutenção é obrigatória antes do fim da fase 2.

### TTL por categoria

- `EndpointOperational`: 30 dias.
- `WorkerFlow`: 15 dias.
- `SystemTask`: 60 dias.

### Purge

- Execução diária com remoção por lotes.
- Remover primeiro os registros mais antigos por `CreatedAtUtc`.
- A estratégia de lotes deve ser configurável para reduzir impacto operacional.

### Job de limpeza

- Executar como tarefa interna agendada diariamente.
- Garantir execução única por ciclo (evitar concorrência entre instâncias).
- Em falha no job, registrar erro estruturado e permitir nova tentativa no próximo ciclo.

### Gatilho de revisão

- Se a tabela `AuditLogs` ultrapassar `X` registros ou `Y` MB, reavaliar TTL e frequência de purge.

### Particionamento futuro

- Não implementar particionamento nesta fase com SQLite.
- Manter diretriz de evolução para particionamento em banco futuro (ex.: PostgreSQL), caso volume e consultas justifiquem.

## Regras de validação

- `CreatedAtUtc` sempre preenchido no backend.
- `ActorUser` obrigatório: usuário autenticado quando houver, ou identificador técnico controlado.
- `Status` deve ser valor válido do enum de auditoria.
- `MetadataJson` deve cumprir contrato mínimo obrigatório (`metadataVersion=1`), incluindo campos globais, campos por categoria, limite de 4 KB e sanitização.

## Critérios de aceite

- Existe plano claro e faseado para auditoria ao longo do projeto.
- A fase 1 (Spec 15) fica explicitamente limitada a infraestrutura (tabela + escrita service/repository), sem alterar serviços existentes.
- `Status` está padronizado como enum persistido como string.
- `MetadataJson` suporta payload operacional rico com regras de segurança.
- `MetadataJson` segue contrato mínimo obrigatório por categoria de evento, com `metadataVersion=1`, limite de 4 KB e política de sanitização/validação.
- Existe política única de falha na escrita de auditoria por fluxo: segunda tentativa imediata e, em nova falha, log estruturado para recuperação futura.
- O contrato canônico atual de `ActorUser` permanece inalterado nesta spec, com evolução planejada e explicitamente delegada para a Spec 21.
- Existe política mínima obrigatória de retenção/manutenção com TTL por categoria, purge diário em lotes e job de limpeza até o fim da fase 2.
- O gatilho de revisão (`X` registros ou `Y` MB) está definido para reavaliar TTL e frequência de purge.
- O gate de qualidade está definido com fluxos obrigatórios por fase e critérios mensuráveis, excluindo apenas o fluxo técnico da Spec 17.
- As futuras specs que adicionarem novos pontos auditáveis devem referenciar esta mini-spec como baseline.

## Testes esperados

- Fase 1:
  - teste de repositório para escrita e consulta por período/ator.
  - teste de conversão enum <-> string para `Status`.
  - teste de validação de `MetadataJson` (JSON válido e bloqueio de campos sensíveis quando aplicável).
- Fases seguintes:
  - testes de integração por fluxo auditado.
  - testes de regressão garantindo ausência de mudança em contratos de API.
  - testes de falha de auditoria garantindo segunda tentativa de escrita e, após nova falha, registro de log estruturado para recuperação futura.
  - testes de contrato de `MetadataJson` por categoria (`EndpointOperational`, `WorkerFlow`, `SystemTask`) validando obrigatórios, tipos e `metadataVersion=1`.
  - testes de limite de tamanho (aceita até 4 KB, rejeita acima de 4 KB).
  - testes de sanitização com denylist e allowlist por categoria.
  - testes de retenção por categoria garantindo expurgo conforme TTL (`EndpointOperational`, `WorkerFlow`, `SystemTask`).
  - testes do purge diário em lotes e da ordenação por `CreatedAtUtc`.
  - testes do gatilho de revisão quando ultrapassar `X` registros ou `Y` MB.
  - testes de robustez do job de limpeza (execução única por ciclo e comportamento em falha).
  - testes de gate por fase validando cobertura de 100% dos fluxos obrigatórios definidos na mini-spec.
  - testes de conformidade de catálogo garantindo ausência de `Action`/`Resource` fora da lista permitida por fase.

## Fora de escopo

- Dashboard de auditoria.
- SIEM/APM externo.
- Arquivamento avançado com storage externo dedicado.
- Reescrita imediata de todos os serviços existentes para instrumentação total em uma única entrega.

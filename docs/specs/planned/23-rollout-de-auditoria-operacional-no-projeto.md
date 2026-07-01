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

### Fase 3 - Cobertura ampliada e padronização

- Implementar auditoria operacional no processamento assíncrono, incluindo eventos de worker/replay/retry/dead-letter, sem duplicar implementação na Spec 17.
- Expandir para outros fluxos de ingestão quando houver ganho operacional claro.
- Evoluir o catálogo de `Action`/`Resource` já fechado na fase 2, mantendo compatibilidade retroativa.
- Adicionar telemetria de qualidade da auditoria (campos faltantes, volume por fluxo, latência de escrita).

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

- Permitir payload operacional mais rico, priorizando estrutura tipada serializada para JSON.
- Campos recomendados por evento:
  - `correlationId`
  - `requestPath`
  - `userId`
  - `provider`
  - `workerState`
  - `httpStatus`
  - `errorCode`
  - `attempt`
- Definir versão de schema do metadata (ex.: `metadataVersion`) para evolução sem quebra.
- Proibir persistência de segredos: token, senha, chave, authorization header e equivalentes.

## Regras de validação

- `CreatedAtUtc` sempre preenchido no backend.
- `ActorUser` obrigatório: usuário autenticado quando houver, ou identificador técnico controlado.
- `Status` deve ser valor válido do enum de auditoria.
- `MetadataJson` deve ser JSON válido quando informado.

## Critérios de aceite

- Existe plano claro e faseado para auditoria ao longo do projeto.
- A fase 1 (Spec 15) fica explicitamente limitada a infraestrutura (tabela + escrita service/repository), sem alterar serviços existentes.
- `Status` está padronizado como enum persistido como string.
- `MetadataJson` suporta payload operacional rico com regras de segurança.
- Existe política única de falha na escrita de auditoria por fluxo: segunda tentativa imediata e, em nova falha, log estruturado para recuperação futura.
- O contrato canônico atual de `ActorUser` permanece inalterado nesta spec, com evolução planejada e explicitamente delegada para a Spec 21.
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

## Fora de escopo

- Dashboard de auditoria.
- SIEM/APM externo.
- Política completa de retenção/arquivamento.
- Reescrita imediata de todos os serviços existentes para instrumentação total em uma única entrega.

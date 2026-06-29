# Mini-spec: Campo de auditoria de origem de inserção em ChatMessages

Número: 16
Status: implementado

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- A tabela `ChatMessages` não identifica claramente quem inseriu/processou a mensagem no backend.
- O campo `Author` representa o autor da mensagem na plataforma de chat, não o ator que realizou a inserção no sistema.

## Comportamento esperado

- Adicionar campo de auditoria em `ChatMessages` para rastrear o usuário/ator que inseriu a mensagem.
- Diferenciar explicitamente o autor do chat (`Author`) do ator de inserção (`InsertedByUser`).
- Preencher o campo em todos os fluxos de entrada (HTTP e worker).

## Superfícies afetadas

- Endpoints: `POST /messages/ingest` (preenchimento da auditoria por usuário autenticado).
- Handlers: `MessageIngestHandler` e serviços compartilhados de processamento.
- Workers/Provedores: `ChatProcessorService` e fluxo assíncrono devem preencher identificador técnico.
- Integrações externas: sem alteração obrigatória.

## Dados e persistência

- Novo campo em `ChatMessages`: `InsertedByUser` (string, obrigatório após migração).
- Estratégia de preenchimento:
  - Fluxo HTTP autenticado: usuário do token.
  - Fluxo worker: usuário autenticado que ativou a sessão do worker (dono da sessão ativa).
- Migration deve tratar dados legados com fallback seguro para registros existentes.
- Índice recomendado em `InsertedByUser` para consultas operacionais/auditoria.

## Contratos de API

- Request: sem novo campo obrigatório para cliente externo nesta fase.
- Response: opcional expor `InsertedByUser` em modelos administrativos; sem obrigatoriedade em contratos públicos atuais.
- Códigos HTTP: sem alteração direta.

## Regras de validação

- `InsertedByUser` nunca pode ser nulo ou vazio após aplicação da migration.
- O valor deve ser derivado de contexto confiável de execução (token/autenticação ou identidade técnica interna).
- O cliente não pode sobrescrever arbitrariamente o valor de auditoria.

## Critérios de aceite

- Nova coluna existe e é preenchida para mensagens novas via HTTP e worker.
- Registros legados ficam consistentes após migração com valor fallback definido.
- Auditoria distingue corretamente `Author` (autor do chat) de `InsertedByUser` (ator de inserção).

## Testes esperados

- Testes de handler para preenchimento correto em fluxo HTTP autenticado.
- Testes de worker para preenchimento com o usuário autenticado que ativou a sessão.
- Testes de migração validando atualização de dados legados.

## Fora de escopo

- Auditoria completa de alteração de mensagens (histórico de versões).
- Exposição obrigatória do campo em todos os responses públicos.

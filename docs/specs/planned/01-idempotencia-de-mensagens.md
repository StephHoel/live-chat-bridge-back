# Mini-spec: Idempotência de mensagens

Número: 01
Status: planejado
Origem: [Issue #44](https://github.com/StephHoel/live-chat-bridge/issues/44)

## Problema

- A chave de idempotência atual inclui `Guid` novo por mensagem, o que impede detectar duplicatas reais.
- O backend pode processar e persistir a mesma mensagem mais de uma vez.
- Payloads de plataformas podem não trazer `messageId` e `userId` consistentes, degradando idempotência.

## Comportamento esperado

- A chave de idempotência deve ser derivada de `Provider + Author + Timestamp`.
- Mensagem já processada com mesma chave deve retornar erro de duplicata.
- Mensagem com mesma chave e `Processed == false` deve ser reprocessada.
- Normalização de entrada deve garantir `messageId` e `userId` com fallback estável antes da idempotência.

## Superfícies afetadas

- Endpoints: `POST /messages/ingest`.
- Handlers: `MessageIngestHandler`.
- Workers/Provedores: sem alteração obrigatória nesta mini-spec.
- Integrações externas: sem alteração.

## Dados e persistência

- Atualizar cálculo de `IdempotencyKey` em `LCB.Domain.Entities.ChatMessageEntity`.
- Garantir consulta por chave no `IMessageRepository` e implementação concreta.
- Não quebrar mensagens já armazenadas no banco local atual.
- Adicionar normalizador de payload para preencher IDs ausentes (`messageId` e `userId`) com fallback determinístico.

## Contratos de API

- Request: sem novos campos.
- Response: manter contrato atual de sucesso/erro.
- Códigos HTTP:
  - `200 OK`: mensagem processada com sucesso.
  - `400 Bad Request`: duplicata já processada (`StatusResultEnum.Duplicate`).

## Regras de validação

- `Provider`, `Author` e `Timestamp` devem existir para gerar a chave.
- Timestamp inválido deve seguir regras atuais de validação da API.
- A normalização deve ocorrer antes de calcular `IdempotencyKey`.

## Critérios de aceite

- Duas mensagens com mesmos `Provider`, `Author` e `Timestamp` não podem ser processadas duas vezes quando a primeira já estiver processada.
- Reenvio de mensagem não processada deve permitir nova tentativa.
- Fluxo de ingestão deve permanecer funcional para mensagens únicas.
- Idempotência deve funcionar mesmo quando payload original não trouxer `messageId` completo.

## Testes esperados

- Teste unitário para geração estável de `IdempotencyKey`.
- Teste de handler para cenário duplicado (erro).
- Teste de handler para cenário de reprocessamento (`Processed == false`).
- Teste unitário para normalização de `messageId`/`userId` com fallback.

## Fora de escopo

- Mudança para persistência durável.
- Alteração de contrato público da rota de ingestão.

# Mini-spec: Processamento real de mensagens no ChatProcessorService

Número: 05
Status: planejado

## Problema

- O `ChatProcessorService` atualmente apenas escreve mensagens no console.
- O pipeline assíncrono do worker não executa regras reais de negócio.

## Comportamento esperado

- Mensagens recebidas do canal devem passar por validação, transformação e processamento de comandos.
- O fluxo do worker deve reutilizar o mesmo caso de uso do ingest HTTP (ou um serviço comum extraído dele), evitando duplicação de regras de negócio.
- Deve haver persistência da mensagem processada e atualização de fila quando aplicável.
- Deve existir tratamento de erro resiliente para não interromper o consumo do canal.

## Superfícies afetadas

- Endpoints: sem alteração direta.
- Handlers: reaproveitamento obrigatório da lógica de `MessageIngestHandler` (diretamente ou via serviço compartilhado).
- Workers/Provedores: `ChatProcessorService`, `ChatWorker`, provedores de live.
- Integrações externas: fonte de eventos de live.

## Dados e persistência

- Converter mensagens do canal para entidade de domínio antes de persistir.
- Aplicar idempotência também no fluxo assíncrono usando a mesma estratégia do fluxo HTTP.
- Para mensagens de provider sem ID nativo, a chave de idempotência deve usar o timestamp da mensagem recebido do provider (normalizado para UTC-3), juntamente com provider e author.
- Registrar status de processamento para observabilidade.

## Semântica de entrega

- O canal interno deve operar com semântica **at-least-once intra-processo**.
- Em falhas pontuais durante o processamento, a mensagem pode ser reprocessada; a idempotência deve impedir efeitos duplicados.
- A implementação atual com canal em memória não garante retenção entre reinícios do processo (sem garantia cross-restart).

## Contratos de API

- Request: não se aplica (fluxo interno).
- Response: não se aplica diretamente.
- Códigos HTTP: sem impacto direto.

## Regras de validação

- Mensagens inválidas devem ser descartadas com log estruturado.
- Falhas de comando não podem derrubar o worker.
- Cancelamento por `CancellationToken` deve ser respeitado.
- Cada mensagem processada deve registrar ao menos: `IdempotencyKey`, `Status`, `Error` (quando houver), `Provider` e `DateTime`.

## Critérios de aceite

- Worker processa mensagens com regras de negócio, não apenas log.
- Worker reutiliza o mesmo caso de uso do ingest HTTP (direto ou via serviço compartilhado), sem duplicação de regra de idempotência/fila/comando.
- Pipeline continua operando após falhas pontuais sem interromper o loop de consumo.
- Em mensagens sem ID nativo de provider, o cálculo de idempotência usa timestamp do provider normalizado para UTC-3.
- Logs permitem rastrear cada mensagem com: `IdempotencyKey`, `Status`, `Error` (quando houver), `Provider` e `DateTime`.
- O comportamento at-least-once intra-processo está explícito e coberto por testes.

## Testes esperados

- Testes unitários de processamento de mensagens válidas e inválidas.
- Testes de resiliência do loop de consumo em exceções.
- Testes de integração com canal em memória.
- Testes que comprovem reaproveitamento/paridade de comportamento entre worker e ingest HTTP para idempotência, fila e comando.
- Testes para mensagens sem ID nativo de provider validando idempotência por `Provider + Author + Timestamp` (timestamp vindo do provider).

## Fora de escopo

- Escalonamento horizontal do worker.
- Mecanismo distribuído de fila externa.

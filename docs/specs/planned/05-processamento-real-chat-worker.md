# Mini-spec: Processamento real de mensagens no ChatProcessorService

Número: 05
Status: planejado

## Problema

- O `ChatProcessorService` atualmente apenas escreve mensagens no console.
- O pipeline assíncrono do worker não executa regras reais de negócio.

## Comportamento esperado

- Mensagens recebidas do canal devem passar por validação, transformação e processamento de comandos.
- Deve haver persistência da mensagem processada e atualização de fila quando aplicável.
- Deve existir tratamento de erro resiliente para não interromper o consumo do canal.

## Superfícies afetadas

- Endpoints: sem alteração direta.
- Handlers: possível reaproveitamento de lógica de `MessageIngestHandler` ou extração para serviço compartilhado.
- Workers/Provedores: `ChatProcessorService`, `ChatWorker`, provedores de live.
- Integrações externas: fonte de eventos de live.

## Dados e persistência

- Converter mensagens do canal para entidade de domínio antes de persistir.
- Aplicar idempotência também no fluxo assíncrono quando houver identificadores equivalentes.
- Registrar status de processamento para observabilidade.

## Contratos de API

- Request: não se aplica (fluxo interno).
- Response: não se aplica diretamente.
- Códigos HTTP: sem impacto direto.

## Regras de validação

- Mensagens inválidas devem ser descartadas com log estruturado.
- Falhas de comando não podem derrubar o worker.
- Cancelamento por `CancellationToken` deve ser respeitado.

## Critérios de aceite

- Worker processa mensagens com regras de negócio, não apenas log.
- Pipeline continua operando após falhas pontuais.
- Métricas/logs permitem rastrear mensagem processada, ignorada ou com erro.

## Testes esperados

- Testes unitários de processamento de mensagens válidas e inválidas.
- Testes de resiliência do loop de consumo em exceções.
- Testes de integração com canal em memória.

## Fora de escopo

- Escalonamento horizontal do worker.
- Mecanismo distribuído de fila externa.

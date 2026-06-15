# Mini-spec: Endpoint HTTP para bootstrap da fila

Número: 07
Status: planejado
Origem: [Issue #33](https://github.com/StephHoel/live-chat-bridge/issues/33)

## Problema

- A UI precisa carregar estado inicial da fila antes de assinar SSE.
- Sem endpoint HTTP, a tela depende apenas de stream em tempo real.

## Comportamento esperado

- Expor endpoint GET para leitura da fila inicial.
- Aceitar filtros `platform` e `channelId`.
- Retornar lista ordenada para bootstrap da UI.

## Superfícies afetadas

- Endpoints: rota HTTP de listagem de fila.
- Handlers: use case de listagem de fila.
- Workers/Provedores: sem alteração direta.
- Integrações externas: sem alteração.

## Dados e persistência

- Consulta deve usar `IQueueRepository` atual.
- Preparar contrato para futura persistência durável.

## Contratos de API

- Request: query string `platform`, `channelId`.
- Response: lista ordenada de participantes da fila.
- Códigos HTTP:
  - `200 OK`: retorno da fila.
  - `400 Bad Request`: query inválida.

## Regras de validação

- `platform` deve estar entre provedores suportados.
- `channelId` obrigatório quando aplicável.

## Critérios de aceite

- Dashboard consegue carregar fila inicial por fetch.
- Ordenação da fila consistente com regra de entrada.

## Testes esperados

- Teste de endpoint com query válida.
- Teste de validação de query inválida.
- Teste de ordenação da lista.

## Fora de escopo

- Canal SSE em si.
- Alterações visuais da UI.

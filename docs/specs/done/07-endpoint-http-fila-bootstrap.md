# Mini-spec: Endpoint HTTP para bootstrap da fila

Número: 07
Status: implementado
Origem: [Issue #33](https://github.com/StephHoel/live-chat-bridge/issues/33)

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- A UI precisa carregar estado inicial da fila antes de assinar SSE.
- Sem endpoint HTTP, a tela depende apenas de stream em tempo real.

## Comportamento esperado

- Expor endpoint GET para leitura da fila inicial.
- Retornar lista ordenada por `CreatedAt` em ordem crescente para bootstrap da UI.
- O endpoint deve ser somente leitura, sem alterar o estado da fila.

## Superfícies afetadas

- Endpoints: rota HTTP de listagem de fila.
- Handlers: use case de listagem de fila.
- Workers/Provedores: sem alteração direta.
- Integrações externas: sem alteração.

## Dados e persistência

- Consulta deve usar `IQueueRepository` atual.
- Não deve introduzir nova lógica de persistência, inserção, remoção ou reordenação da fila.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/planned/25-isolamento-da-fila-por-usuario-e-delecao-logica.md](../planned/25-isolamento-da-fila-por-usuario-e-delecao-logica.md): a leitura implementada nesta spec é transitória sobre o modelo global atual e deverá evoluir para filtro por usuário/sessão ativa e exclusão de itens logicamente deletados.

## Contratos de API

- Request: sem body e sem parâmetros obrigatórios.
- Response: lista ordenada de participantes da fila, envelopada no padrão `Result<T>` do projeto.
- Códigos HTTP:
  - `200 OK`: retorno da fila.
  - `401 Unauthorized`: ausência ou invalidade de token.

## Regras de ordenação

- A lista deve ser ordenada por `CreatedAt` em ordem crescente.

## Critérios de aceite

- Dashboard consegue carregar fila inicial por fetch.
- Ordenação da fila consistente com `CreatedAt`.
- A consulta não altera dados persistidos da fila.

## Testes esperados

- Teste de endpoint autenticado.
- Teste de autenticação obrigatória.
- Teste de ordenação da lista.

## Fora de escopo

- Canal SSE em si.
- Canal de transmissão (`channelId`) e filtros associados.
- Inserção de participantes na fila.
- Remoção, seleção, avanço ou qualquer mutação de estado da fila.
- Alterações em workers, provedores ou regras de entrada na fila.
- Alterações visuais da UI.

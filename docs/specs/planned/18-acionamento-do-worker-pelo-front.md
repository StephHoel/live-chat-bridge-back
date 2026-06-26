# Mini-spec: Acionamento do worker pelo front

Número: 18
Status: planejado

## Problema

- Hoje o worker inicia junto com a aplicação e passa a operar sem um gesto explícito do front.
- Isso dificulta controle operacional, previsibilidade de início e parada, e a separação entre API disponível e worker efetivamente ativo.
- Falta um contrato claro para o front decidir quando o worker deve começar a processar eventos.

## Objetivo

- Permitir que o worker só funcione após um acionamento explícito vindo do front.
- Definir um contrato simples e seguro para iniciar, consultar e, se necessário, interromper o worker.
- Preservar o comportamento atual da API HTTP fora desse controle operacional.

## Opções de acionamento avaliadas

1. `POST /worker/start` e `POST /worker/stop` protegidos por autenticação.
2. Um endpoint único de comando operacional, por exemplo `POST /worker/control`, com ação no corpo (`start` / `stop`).
3. Canal em tempo real com SignalR/WebSocket para comando do front ao backend.

## Recomendação

- Preferir `POST /worker/start` e `POST /worker/stop` como contrato explícito e fácil de testar.
- Complementar com `GET /worker/status` para o front exibir se o worker está ativo, parado ou em transição.
- Manter o acionamento restrito a um front autenticado, sem início automático no boot, exceto se houver decisão explícita diferente em ambiente futuro.

## Comportamento esperado

- O worker inicia em estado inativo.
- O front envia um comando explícito para habilitar o worker.
- O backend registra e expõe o estado operacional atual do worker.
- O worker pode ser interrompido por comando explícito, sem derrubar a API HTTP inteira.
- Comandos repetidos de start/stop devem ser idempotentes ou responder com estado atual.

## Superfícies afetadas

- Endpoints: novos endpoints de controle do worker e endpoint de status.
- Handlers: orquestração de start/stop/status.
- Workers/Provedores: `ChatWorker` e possivelmente serviços auxiliares de ciclo de vida.
- Integrações externas: front da aplicação ou painel administrativo.

## Dados e persistência

- Definir se o estado operacional do worker será apenas em memória ou persistido.
- Se persistido, registrar o último estado conhecido e o horário da transição.
- Não persistir payloads sensíveis de comando.

## Contratos de API

- `POST /worker/start`: habilita o worker.
- `POST /worker/stop`: interrompe o worker.
- `GET /worker/status`: retorna o estado atual do worker.
- Códigos HTTP esperados:
  - `200 OK`: comando executado ou status retornado.
  - `401 Unauthorized`: front não autenticado.
  - `409 Conflict`: comando redundante, se o estado já estiver no destino.
  - `503 Service Unavailable`: worker indisponível por falha operacional.

## Regras de validação

- O front precisa estar autenticado para acionar o worker.
- Start só pode ocorrer a partir de um estado inativo.
- Stop só pode ocorrer a partir de um estado ativo.
- O worker não deve aceitar processamento antes do comando de start.
- Estados intermediários devem ser bem definidos para evitar corrida entre início e consumo.

## Critérios de aceite

- O worker não processa mensagens até receber acionamento explícito do front.
- O front consegue iniciar o worker e consultar seu status.
- O front consegue interromper o worker sem afetar a API HTTP principal.
- Chamadas repetidas de start/stop não quebram o estado operacional.

## Testes esperados

- Teste de start do worker a partir de estado inativo.
- Teste de stop do worker a partir de estado ativo.
- Teste de status refletindo transição correta.
- Teste de start/stop redundante.
- Teste de bloqueio quando o front não estiver autenticado.

## Fora de escopo

- Orquestração distribuída entre múltiplas instâncias.
- Escalonamento automático do worker.
- Fila externa para comandos operacionais.

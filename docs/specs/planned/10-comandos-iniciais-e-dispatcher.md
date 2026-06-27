# Mini-spec: Comandos iniciais e registro no dispatcher

Número: 10
Status: planejado
Origem:

- [Issue #37](https://github.com/StephHoel/live-chat-bridge/issues/37)
- [Issue #38](https://github.com/StephHoel/live-chat-bridge/issues/38)
- [Issue #39](https://github.com/StephHoel/live-chat-bridge/issues/39)
- [Issue #40](https://github.com/StephHoel/live-chat-bridge/issues/40)

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- Comandos principais ainda não estão completos e registrados de forma integrada.
- Falta comportamento padronizado para comandos desconhecidos.

## Comportamento esperado

- Implementar handlers para `!fila`, `!pontos` e `!tada`.
- Registrar os handlers no dispatcher.
- Garantir que apenas mensagens iniciadas com `!` sejam tratadas como comando.
- Retornar resultado para comando desconhecido (`ignored`/`unknown_command`).

## Superfícies afetadas

- Endpoints: `POST /messages/ingest`.
- Handlers: handlers de comando e dispatcher.
- Workers/Provedores: pode reaproveitar fluxo no processamento assíncrono.
- Integrações externas: event store para `command_result` e `ui_effect`.

## Dados e persistência

- `!fila`: consulta posição e tempo de entrada.
- `!pontos`: consulta saldo no `PointsRepository`.
- `!tada`: grava evento de efeito visual com defaults.

## Contratos de API

- Request: sem novo endpoint.
- Response: resultado de comando conforme padrão atual.
- Códigos HTTP:
  - `200 OK`: mensagem processada e comando tratado.
  - `400 Bad Request`: payload inválido.

## Regras de validação

- Comando só processa quando o prefixo `!` estiver presente.
- Campos obrigatórios por comando devem ser validados.

## Critérios de aceite

- Dispatcher resolve corretamente os 3 comandos.
- `!fila` retorna posição quando usuário estiver na fila.
- `!pontos` retorna saldo correto (incluindo `0`).
- `!tada` gera `ui_effect` consumível pela UI.

## Testes esperados

- Testes unitários por handler.
- Testes unitários do dispatcher para roteamento de comandos.
- Teste para comando desconhecido.

## Fora de escopo

- Comandos avançados fora do trio inicial.
- Efeitos visuais adicionais além de confetti.

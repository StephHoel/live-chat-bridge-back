# Mini-spec: Use case de pontuação e evento points_updated

Número: 09
Status: planejado
Origem: [Issue #36](https://github.com/StephHoel/live-chat-bridge/issues/36)

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- Falta caso de uso transacional para aplicar regras e atualizar saldo.
- UI não recebe evento padronizado de atualização de pontos.

## Comportamento esperado

- Criar use case de pontuação (`AwardPointsUseCase`).
- Calcular delta via rules engine.
- Persistir saldo no repositório de pontos.
- Emitir evento `points_updated` com novo saldo.

## Superfícies afetadas

- Endpoints: indireto via fluxos que pontuam usuário.
- Handlers: handlers que acionam pontuação.
- Workers/Provedores: origem de eventos que geram pontuação.
- Integrações externas: barramento/event store interno.

## Dados e persistência

- Atualizar saldo por `platform + channelId + userId`.
- Evento deve carregar dados mínimos para UI reagir sem refresh.

## Contratos de API

- Request: não define endpoint obrigatório.
- Response: não se aplica diretamente.
- Códigos HTTP: sem impacto direto.

## Regras de validação

- Não permitir delta inválido (NaN ou overflow).
- Garantir consistência entre saldo persistido e payload do evento.

## Critérios de aceite

- Ao pontuar, saldo é atualizado corretamente.
- Evento `points_updated` é emitido com saldo final.
- UI pode reagir ao evento sem recarregar página.

## Testes esperados

- Teste unitário do use case com cenários de sucesso.
- Teste de emissão de evento após persistência.
- Teste de consistência do saldo no evento.

## Fora de escopo

- Transporte SSE específico do frontend.
- Estratégia distribuída de event bus.

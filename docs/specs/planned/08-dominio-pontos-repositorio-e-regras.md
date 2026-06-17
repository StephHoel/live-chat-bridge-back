# Mini-spec: Domínio de pontos, repositório e regras por plataforma

Número: 08
Status: planejado
Origem: [Issue #34](https://github.com/StephHoel/live-chat-bridge/issues/34) e [Issue #35](https://github.com/StephHoel/live-chat-bridge/issues/35)

## Problema

- Não há estrutura de domínio para pontos por usuário/canal.
- Regras de pontuação por plataforma e tipo de integração ainda não existem.

## Comportamento esperado

- Criar entidade de saldo de pontos com contexto de plataforma/canal/usuário.
- Criar contrato de repositório de pontos e implementação persistente compatível com EF Core.
- Criar política por plataforma e engine de regras para cálculo de delta.

## Superfícies afetadas

- Endpoints: impacto indireto em comandos/consultas de pontos.
- Handlers: handlers e use cases que consultam ou creditam pontos.
- Workers/Provedores: podem usar regras ao processar eventos.
- Integrações externas: sem alteração obrigatória.

## Dados e persistência

- Modelo `PointsBalance` com `platform`, `channelId`, `userId`, `points`, `updatedAt`.
- `PointsRepository` com operações de leitura e crédito de pontos.
- Implementação inicial alinhada ao padrão atual de persistência durável (EF Core + SQLite), com plano de provider PostgreSQL futuro.

## Contratos de API

- Request: não define rota nova por si só.
- Response: não define rota nova por si só.
- Códigos HTTP: sem impacto direto nesta mini-spec.

## Regras de validação

- Saldo inexistente deve iniciar em `0`.
- Delta depende de `platform` + `integrationType`.
- `integrationType` não suportado deve cair em regra default segura.

## Critérios de aceite

- Consulta de saldo retorna `0` quando usuário não existe.
- Crédito acumula corretamente no mesmo contexto.
- Delta varia conforme plataforma e tipo de integração.

## Testes esperados

- Testes de repositório persistente (SQLite em memória para ambiente de teste).
- Testes unitários da política de pontos por plataforma.
- Testes da engine para combinações de integration type.

## Fora de escopo

- Persistência relacional definitiva.
- Painel de administração de pontos.

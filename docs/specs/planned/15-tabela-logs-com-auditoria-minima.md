# Mini-spec: Tabela de logs com auditoria mínima

Número: 15
Status: planejado

## Problema

- O sistema possui observabilidade por logger, mas não há trilha persistida mínima para consultas de auditoria operacional.
- Sem tabela dedicada, fica difícil rastrear eventos relevantes por ator e momento em ambientes com retenção curta de logs.

## Comportamento esperado

- Implementar tabela de logs de auditoria com persistência durável.
- Todo registro de auditoria deve conter, no mínimo, data/hora de criação e usuário ator.
- Adotar nomenclatura explícita para auditoria mínima: `CreatedAtUtc` e `ActorUser`.
- Permitir expansão futura de colunas sem quebra de compatibilidade.

## Superfícies afetadas

- Endpoints: sem mudança de contrato público obrigatória nesta fase.
- Handlers: pontos de uso que geram ações auditáveis podem publicar entradas na tabela.
- Workers/Provedores: eventos operacionais relevantes podem gerar auditoria quando aplicável.
- Integrações externas: sem obrigatoriedade.

## Dados e persistência

- Nova entidade/tabela de auditoria (ex.: `AuditLogs`).
- Campos mínimos obrigatórios:
  - `Id` (Guid)
  - `CreatedAtUtc` (DateTime)
  - `ActorUser` (string)
- Campos recomendados para utilidade operacional:
  - `Action` (string)
  - `Resource` (string)
  - `Status` (string)
  - `MetadataJson` (string opcional)
- Criar migration para inclusão da tabela e índices básicos (`CreatedAtUtc`, `ActorUser`).

## Contratos de API

- Request: não se aplica nesta mini-spec.
- Response: não se aplica nesta mini-spec.
- Códigos HTTP: sem impacto direto.

## Regras de validação

- `CreatedAtUtc` deve ser sempre preenchido pelo backend.
- `ActorUser` deve ser preenchido com usuário autenticado quando houver; em execução de sistema, usar identificador técnico controlado (ex.: `system:worker`).
- Nunca persistir tokens, senhas ou segredos na tabela de auditoria.

## Critérios de aceite

- A tabela de auditoria existe com os campos mínimos definidos.
- Entradas de auditoria persistem `CreatedAtUtc` e `ActorUser` de forma consistente.
- Índices básicos permitem consulta por período e por usuário ator.
- Não há vazamento de dados sensíveis no conteúdo auditado.

## Testes esperados

- Testes de repositório para criação e consulta por período.
- Testes para validação de preenchimento de `CreatedAtUtc` e `ActorUser`.
- Testes garantindo ausência de dados sensíveis persistidos em auditoria.

## Fora de escopo

- Dashboard de auditoria.
- Retenção, rotação e arquivamento avançado de logs.
- Integração com SIEM/APM externo.

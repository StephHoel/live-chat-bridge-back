# Mini-spec: Tabela de logs com auditoria mínima

Número: 15
Status: implementado

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- O sistema possui observabilidade por logger, mas não há trilha persistida mínima para consultas de auditoria operacional.
- Sem tabela dedicada, fica difícil rastrear eventos relevantes por ator e momento em ambientes com retenção curta de logs.

## Decisão de recorte desta spec

- Esta mini-spec implementa somente a fundação da auditoria: tabela e mecanismo de escrita via `service -> repository`.
- Esta mini-spec não deve alterar handlers, workers, serviços ou fluxos de negócio já implementados.
- A adoção da auditoria nos fluxos do projeto será faseada em mini-spec específica de rollout.

## Comportamento esperado

- Implementar tabela de logs de auditoria com persistência durável.
- Implementar contrato mínimo de escrita de auditoria por serviço dedicado que delega ao repositório.
- Todo registro de auditoria deve conter, no mínimo, data/hora de criação e usuário ator.
- Adotar nomenclatura explícita para auditoria mínima: `CreatedAtUtc` e `ActorUser`.
- Padronizar `Status` como enum de domínio, persistido como string no banco.
- Permitir `MetadataJson` com payload operacional mais rico e evolutivo.

## Superfícies afetadas

- Endpoints: sem mudança de contrato público obrigatória nesta fase.
- Handlers: sem alteração nesta fase.
- Workers/Provedores: sem alteração nesta fase.
- Serviços: adição de serviço de auditoria para gravação (`service -> repository`).
- Repositórios/Persistência: nova entidade/tabela de auditoria e repositório dedicado.
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
  - `Status` (enum no domínio, persistido como string)
  - `MetadataJson` (string opcional)
- Criar migration para inclusão da tabela e índices básicos (`CreatedAtUtc`, `ActorUser`).
- Recomenda-se índice composto `Action + CreatedAtUtc` para consultas operacionais.

## Contratos internos (não HTTP)

- Criar enum dedicado para status de auditoria (ex.: `AuditLogStatusEnum`) com persistência como string (`HasConversion<string>()`).
- Criar serviço de auditoria (ex.: `AuditLogService`) responsável por validar e montar o registro antes de persistir.
- Criar repositório de auditoria (ex.: `IAuditLogRepository`) responsável pela escrita/consulta.

## Contratos de API

- Request: não se aplica nesta mini-spec.
- Response: não se aplica nesta mini-spec.
- Códigos HTTP: sem impacto direto.

## Regras de validação

- `CreatedAtUtc` deve ser sempre preenchido pelo backend.
- `ActorUser` deve ser preenchido com usuário autenticado quando houver; em execução de sistema, usar identificador técnico controlado (ex.: `system:worker`).
- `Status` deve aceitar apenas valores válidos do enum de auditoria.
- `MetadataJson` deve conter JSON válido quando informado.
- `MetadataJson` pode conter payload operacional mais rico, mas sem segredos.
- Nunca persistir tokens, senhas ou segredos na tabela de auditoria.

## Critérios de aceite

- A tabela de auditoria existe com os campos mínimos definidos.
- Entradas de auditoria persistem `CreatedAtUtc` e `ActorUser` de forma consistente.
- `Status` é modelado como enum no domínio e persistido como string no banco.
- Existe serviço de auditoria chamando o repositório (`service -> repository`) para gravação.
- Índices básicos permitem consulta por período e por usuário ator.
- Não há vazamento de dados sensíveis no conteúdo auditado.
- Não há alteração em serviços, handlers, workers e fluxos de negócio existentes nesta entrega.

## Testes esperados

- Testes de repositório para criação e consulta por período.
- Testes para validação de preenchimento de `CreatedAtUtc` e `ActorUser`.
- Testes de conversão enum <-> string para `Status`.
- Testes de validação de `MetadataJson` para JSON válido.
- Testes garantindo ausência de dados sensíveis persistidos em auditoria.

## Fora de escopo

- Instrumentação dos fluxos já existentes (handlers, workers, serviços de negócio).
- Dashboard de auditoria.
- Retenção, rotação e arquivamento avançado de logs.
- Integração com SIEM/APM externo.

## Decisões tomadas durante a implementação desta fase

- Enum de status adotado como `AuditLogStatusEnum` com valores iniciais `Success`, `Failure`, `Warning` e `Info`.
- Persistência de `Status` configurada com `HasConversion<string>()` no EF Core.
- Estrutura de escrita implementada via `IAuditLogService` -> `IAuditLogRepository`.
- `MetadataJson` validado como JSON no serviço de auditoria antes da persistência.
- Conteúdo sensível em `MetadataJson` é bloqueado no serviço por validação semântica de termos proibidos (`token`, `password`, `secret`, `authorization`, `api_key`, `apikey`, `jwt`).

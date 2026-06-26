# Mini-spec: Consolidação do modelo de mensagem entre HTTP e worker

Número: 04
Status: implementado

## Problema

- Existem modelos de mensagem com papéis mistos entre fluxo HTTP e fluxo assíncrono do worker.
- Isso aumenta acoplamento, risco de mapeamento incorreto e inconsistência de regras de negócio.

## Comportamento esperado

- Definir fronteiras claras para Entity, DTO e Model no fluxo de mensagens.
- Nomear explicitamente o tipo atual do canal do worker como LCB.Domain.Models.ChatMessageModel (tratado como WorkerInput neste contexto).
- Fluxo do worker deve converter WorkerInput para LCB.Domain.Entities.ChatMessageEntity por meio de mapeador dedicado antes de persistência/regras.
- Fluxo HTTP e worker devem compartilhar regras centrais de normalização/validação quando aplicável.

## Superfícies afetadas

- Endpoints: POST /messages/ingest (alinhamento de contrato interno).
- Handlers: MessageIngestHandler.
- Workers/Provedores: ChatWorker, ChatProcessorService, provedores de live.
- Integrações externas: provedores de chat (TikTok e futuros).

## Dados e persistência

- Definir mapeamento explícito entre WorkerInput (ChatMessageModel) e LCB.Domain.Entities.ChatMessageEntity.
- Regra única de normalização temporal: todos os timestamps de entrada (HTTP e worker) devem ser convertidos e persistidos em UTC-3.
- Garantir compatibilidade com chave de idempotência e regras de fila.
- Evitar persistir tipos destinados apenas a transporte/response.

## Contratos de API

- Request: sem mudança obrigatória imediata.
- Response: migrar retorno de ingestão para Model de API (sem expor Entity de persistência).
- Códigos HTTP: sem alteração planejada nesta mini-spec.

## Regras de validação

- Normalização de campos comuns (Author, Message, Timestamp, Provider).
- Timestamp deve ser sempre UTC-3 após mapeamento, independente da origem.
- Tratamento consistente de valores ausentes/inválidos entre canais de entrada.

## Critérios de aceite

- Todos os fluxos de mensagem passam por conversões explícitas por camada.
- Existe mapeador dedicado WorkerInput -> ChatMessageEntity no fluxo assíncrono.
- Não há uso de Model de API como tipo de persistência.
- O retorno de POST /messages/ingest usa Model de API em vez de Entity.
- Regras de negócio essenciais funcionam igual para entrada HTTP e worker.

## Testes esperados

- Testes unitários de mapeamento worker -> entidade.
- Testes de integração do pipeline assíncrono com validação de regras comuns.
- Testes de regressão para ingestão HTTP.

## Fora de escopo

- Redesign de payload externo dos provedores.
- Alteração visual/contratual para frontend.

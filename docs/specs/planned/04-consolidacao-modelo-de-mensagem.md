# Mini-spec: Consolidação do modelo de mensagem entre HTTP e worker

Número: 04
Status: planejado

## Problema

- Existem modelos de mensagem com papéis mistos entre fluxo HTTP e fluxo assíncrono do worker.
- Isso aumenta acoplamento, risco de mapeamento incorreto e inconsistência de regras de negócio.

## Comportamento esperado

- Definir fronteiras claras para `Entity`, `DTO` e `Model` no fluxo de mensagens.
- Fluxo do worker deve converter payload de provedor para DTO interno e então para `Entity` antes de persistência/regras.
- Fluxo HTTP e worker devem compartilhar regras centrais de normalização/validação quando aplicável.

## Superfícies afetadas

- Endpoints: `POST /messages/ingest` (alinhamento de contrato interno).
- Handlers: `MessageIngestHandler`.
- Workers/Provedores: `ChatWorker`, `ChatProcessorService`, provedores de live.
- Integrações externas: provedores de chat (TikTok e futuros).

## Dados e persistência

- Definir mapeamento explícito entre tipo de entrada do worker e `LCB.Domain.Entities.ChatMessage`.
- Garantir compatibilidade com chave de idempotência e regras de fila.
- Evitar persistir tipos destinados apenas a transporte/response.

## Contratos de API

- Request: sem mudança obrigatória imediata.
- Response: manter contrato de API pública.
- Códigos HTTP: sem alteração planejada nesta mini-spec.

## Regras de validação

- Normalização de campos comuns (`Author`, `Message`, `Timestamp`, `Provider`).
- Tratamento consistente de valores ausentes/inválidos entre canais de entrada.

## Critérios de aceite

- Todos os fluxos de mensagem passam por conversões explícitas por camada.
- Não há uso de `Model` de API como tipo de persistência.
- Regras de negócio essenciais funcionam igual para entrada HTTP e worker.

## Testes esperados

- Testes unitários de mapeamento worker -> entidade.
- Testes de integração do pipeline assíncrono com validação de regras comuns.
- Testes de regressão para ingestão HTTP.

## Fora de escopo

- Redesign de payload externo dos provedores.
- Alteração visual/contratual para frontend.

# Mini-spec: Domínio de pontos, repositório e regras por plataforma

Número: 08
Status: planejado
Origem: [Issue #34](https://github.com/StephHoel/live-chat-bridge/issues/34) e [Issue #35](https://github.com/StephHoel/live-chat-bridge/issues/35)

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- Não há estrutura de domínio para pontos por usuário/canal.
- Regras de pontuação por plataforma e tipo de integração ainda não existem.

## Comportamento esperado

- Criar entidade de saldo de pontos com contexto de plataforma/canal/usuário.
- Criar contrato de repositório de pontos e implementação persistente compatível com EF Core.
- Criar política por plataforma e engine de regras para cálculo de delta.
- Garantir atualização atômica de saldo via estratégia de upsert.

## Superfícies afetadas

- Endpoints: impacto indireto em comandos/consultas de pontos.
- Handlers: handlers e use cases que consultam ou creditam pontos.
- Workers/Provedores: podem usar regras ao processar eventos.
- Integrações externas: sem alteração obrigatória.

## Dados e persistência

- Modelo `PointsBalance` com `provider`, `channelId`, `userId`, `points`, `isActive`, `updatedAt`.
  - `userId` representa o `author` da `ChatMessage`.
  - `channelId` representa o username da live ativa (streamer) de onde a mensagem/evento foi recebido.
  - `points` deve ser `long` e não negativo.
  - `isActive` deve ser `bool` com valor inicial `true`.
  - Chave única de saldo ativo: `provider + channelId + userId + isActive`.
  - Deve existir no máximo 1 registro ativo por combinação `provider + channelId + userId`.
  - Pode existir N registros inativos para histórico da mesma combinação.
- Modelo `PointsTransaction` persistente para trilha de alterações de saldo (`credit`, `debit`, `clear`) com vínculo ao contexto (`provider + channelId + userId`).
  - Campos mínimos obrigatórios: `provider`, `channelId`, `userId`, `points`, `situation` (`credit` ou `debit`), `transactionDateTime`.
- `PointsRepository` com operações de leitura e atualização incremental atômica de saldo.
  - Estratégia de persistência de saldo: upsert.
- `PointsTransactionRepository` para registro de transações de pontos.
- `PointsService` de domínio para orquestração de regras e persistência de transações no backend.
- Implementação inicial alinhada ao padrão atual de persistência durável (EF Core + SQLite), com plano de provider PostgreSQL futuro.

## Contratos de API

- Request: não define rota nova por si só.
- Response: não define rota nova por si só.
- Códigos HTTP: sem impacto direto nesta mini-spec.

## Regras de validação

- Saldo inexistente deve iniciar em `0`.
- Delta depende de `provider` + `integrationType`.
- Lista inicial de `integrationType`: `message`, `like`.
- Fonte de providers suportados: providers ativos do streamer na sessão do worker, validados contra enum de providers suportados pelo sistema.
- `integrationType` não suportado não deve adicionar pontos e deve registrar auditoria para observabilidade.
- Plataforma/provider não suportado deve ser ignorado sem crédito de pontos e deve registrar auditoria para observabilidade.
- Pontuação deve permanecer inteira e não negativa.
- A pontuação deve ser aplicada no processamento da mensagem; mensagens já processadas não podem pontuar novamente.
- Operação `clear` deve preservar trilha transacional e aplicar desativação lógica de saldo (`isActive = false`), sem remoção física obrigatória.
- Após `clear`, novo evento deve criar um novo saldo ativo para o usuário/contexto.
- Débito acima do saldo disponível não deve ser permitido.

## Critérios de aceite

- Consulta de saldo retorna `0` quando usuário não existe.
- Crédito acumula corretamente no mesmo contexto.
- Delta varia conforme plataforma e tipo de integração.
- `clear` desativa saldo atual e o próximo evento cria novo registro ativo no mesmo contexto.
- Débito acima do saldo é rejeitado sem alterar o saldo persistido.

## Testes esperados

- Testes de repositório persistente (SQLite em memória para ambiente de teste).
- Testes unitários da política de pontos por plataforma.
- Testes da engine para combinações de integration type.
- Testes de concorrência para garantir upsert com unicidade de saldo ativo.
- Testes para garantir criação de novo saldo ativo após `clear` e manutenção de histórico inativo.
- Testes para rejeição de débito acima do saldo.

## Fora de escopo

- Persistência relacional definitiva.
- Painel de administração de pontos.

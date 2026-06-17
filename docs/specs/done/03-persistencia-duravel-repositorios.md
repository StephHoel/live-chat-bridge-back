# Mini-spec: Persistência durável para repositórios

Número: 03
Status: concluída

## Problema

- O sistema perde mensagens, fila e usuários ao reiniciar porque a persistência é somente em memória.
- Não há base para operação confiável entre reinícios.

## Comportamento esperado

- Repositórios de mensagens, fila e usuários devem persistir dados de forma durável.
- Reinício da aplicação não pode apagar estado de negócio essencial.
- Camada de aplicação deve continuar dependente de contratos do domínio.

## Superfícies afetadas

- Endpoints: `POST /auth/login`, `POST /messages/ingest` e demais que dependam de repositórios.
- Handlers: `LoginHandler`, `MessageIngestHandler` e handlers que acessam repositórios.
- Workers/Provedores: impactados indiretamente quando persistirem mensagens.
- Integrações externas: SQLite (implementado) com estratégia futura para PostgreSQL.

## Dados e persistência

- Mapear entidades de domínio para estrutura persistida (`User`, `Queue`, `ChatMessage`).
- Definir estratégia de migração da base atual (cold start ou seed controlado).
- Definir índices mínimos: e-mail de usuário, chave de idempotência, usuário em fila.

### Decisão arquitetural planejada

- Adotar a **opção C (híbrida)**: `Entity Framework Core` para mapeamento/migrations e repositórios como camada explícita de acesso no projeto `Infrastructure`.
- Manter `Application` dependente apenas de contratos do `Domain`; a troca de provider de banco deve ocorrer por configuração e DI.
- Evitar SQL acoplado nos handlers; consultas específicas devem ficar nos repositórios.

### Estratégia de banco local e online

- **Fase 1 (local, desenvolvimento):** SQLite com arquivo `.db` local.
- **Fase 2 (online, produção):** PostgreSQL em servidor gerenciado, sem mudança de contrato de aplicação. (_Implementação em spec futura; provider definido para orientar decisions de tipos e migrations desde já._)
- Definir `ConnectionStrings` com seleção de provider por configuração (`Database:Provider`) para permitir troca sem alteração de regra de negócio.

### Planejamento de schema relacional (pré-implementação)

- Tabelas seguem o nome das entidades do domínio: `Users`, `Queues`, `ChatMessages`.
- Chaves e restrições:
  - `Users.Email` único.
  - `Queues.User` único (um usuário por fila ativa).
  - `ChatMessages.IdempotencyKey` único.
- Índices mínimos:
  - `Users(Email)`.
  - `Queues(User)`.
  - `ChatMessages(IdempotencyKey)`.
  - `ChatMessages(Processed, Timestamp)` para consultas operacionais.
- Auditoria mínima: `CreatedAt` e `UpdatedAt` nas entidades persistidas.

### Estratégia de migração entre ambientes

- Migrations versionadas no repositório (EF Core) como fonte única de evolução do schema.
- Ambiente local inicia com `dotnet ef database update` sobre SQLite.
- Ambiente online aplica as mesmas migrations no provider escolhido, validando compatibilidade de tipos e índices.
- Seeds devem ser controlados e idempotentes, sem dados de teste fixos em repositório de produção.

### Compatibilidade e riscos conhecidos

- Diferenças de tipo entre SQLite e banco online (ex.: precisão de data/hora e collation) devem ser cobertas por testes de integração.
- Consultas críticas de idempotência e fila precisam de validação de desempenho após criação de índices.
- Operações de ingestão devem permanecer transacionais para evitar estado parcial em falhas.

## Contratos de API

- Request: sem mudanças obrigatórias.
- Response: sem mudanças obrigatórias.
- Códigos HTTP: manter códigos atuais, exceto falhas operacionais de infraestrutura (`500`).

## Regras de validação

- Gravação deve ser transacional para operações críticas de ingestão.
- Falhas de persistência devem ser logadas com correlação.

## Critérios de aceite

- Dados sobrevivem a reinício da API.
- Login e ingestão continuam funcionando com repositórios persistentes.
- Consultas por idempotência e fila mantêm comportamento funcional.

## Testes esperados

- Testes de integração para CRUD básico de usuários, fila e mensagens.
- Teste de reinicialização validando durabilidade.
- Testes de erro de infraestrutura com fallback de resposta.
- Testes de integração rodando ao menos em SQLite (local) e em um provider online alvo (pipeline/ambiente controlado).
- Testes de migrations: banco vazio -> última migration aplicada com sucesso.

## Fora de escopo

- Estratégias avançadas de replicação/alta disponibilidade.
- Dashboard administrativo.

## Resultado da implementação

- Repositórios em memória removidos da runtime.
- `UserRepository`, `QueueRepository` e `ChatMessageRepository` implementados sobre EF Core.
- Migrations versionadas adicionadas em `src/LCB.Infrastructure/Data/Migrations`.
- Aplicação de migrations no startup da API para manter schema alinhado no ambiente local.

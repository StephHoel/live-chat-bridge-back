# CHANGELOG

## [v0.3.0] - 2026-06-23

### ✨ Funcionalidades

- **Idempotência de mensagens** (Spec 01) - Correção da chave de idempotência e fluxo de reprocessamento
  - `IdempotencyKey` agora derivada de `Provider + Author + Timestamp` normalizado em UTC (sem `Guid`)
  - Mensagem com mesma chave e `Processed == true` retorna `400 Bad Request` com `StatusResultEnum.Duplicate`
  - Mensagem com mesma chave e `Processed == false` é reprocessada via `UpdateAsync` em vez de criar duplicata
  - Normalização de `Author` (trim) e `Timestamp` (conversão para UTC) antes do cálculo da chave
  - Campo `Timestamp` em `ChatMessageEntity` alterado para `DateTime?` para suportar payloads sem timestamp

### 🧪 Testes

- Novos testes unitários para semântica de `IdempotencyKey` em `ChatMessageEntityTests`
- Testes de handler ampliados: cenário de reprocessamento (`Processed == false`) e normalização de Author/Timestamp
- `FakeMessageRepository` em `MessageIngestHandlerTests` agora filtra por chave corretamente e rastreia `UpdateCalls`

### 🐛 Correções

- Chave de idempotência não incluía mais `Guid` por mensagem — todas as mensagens eram únicas mesmo sendo duplicatas

## [v0.2.0] - 2026-06-22

### ✨ Funcionalidades

- **Persistência durável** (Spec 03) - Repositórios de usuários, fila e mensagens com EF Core + SQLite
  - Migrations versionadas para evolução controlada do schema
  - Índices otimizados para queries principais (email, idempotência, usuários em fila)
  - Preparação arquitetural para migração futura para PostgreSQL
  
- **Observabilidade e tratamento de erros** (Spec 13) - Refatoração transversal
  - `OperationExecutor` centraliza padrão de logging e erro
  - Logs estruturados com correlação por request
  - Remoção de `Console.WriteLine` em componentes de integração
  - Padronização de tratamento de erros em handlers, serviços e workers

### 🔧 Melhorias Técnicas

- Estrutura de testes com padrão de fábrica (xUnit)
- Middleware de correlação para rastreabilidade
- Converter JSON tolerante para DateTime nas entradas HTTP
- Canais assíncronos internos (`System.Threading.Channels`) para worker

### 🐛 Problemas Conhecidos

- **Idempotência quebrada** - Chave de idempotência atual inclui Guid novo por mensagem (Spec 01 planejada)
- **Autenticação provisória** - Não valida senha (Spec 02 planejada)
- **Lógica de processamento** - `ChatProcessorService` ainda sem implementação real

### 📋 Próximas Prioridades

1. Spec 01 - Idempotência de mensagens
2. Spec 02 - Autenticação com senha
3. Spec 04 - Consolidação modelo de mensagem (HTTP ↔ Worker)
4. Spec 05 - Lógica real do chat worker

---

## [v0.1.0] - Inicial

### ✨ Funcionalidades Base

- Login por email com JWT (`POST /auth/login`)
- Ingestão de mensagens (`POST /messages/ingest`)
- Detecção e dispatch de comandos simples (`!fila`, `!comando`)
- Worker background com suporte TikTok Live (`TikTokLive_Sharp`)
- Fila de usuários
- Comunicação assíncrona interna via channels
- Logging customizado com `TemplateLogger`

### 📦 Stack

- .NET 9 com ASP.NET Core Minimal API
- xUnit para testes
- Swashbuckle/Swagger em desenvolvimento

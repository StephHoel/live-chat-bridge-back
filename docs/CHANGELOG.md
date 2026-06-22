# CHANGELOG

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

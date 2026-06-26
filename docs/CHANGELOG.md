# CHANGELOG

## [v0.6.0] - 2026-06-25

### ✨ Funcionalidades

- **Processamento real no worker de chat** (Spec 05)
  - `ChatProcessorService` deixou de ser apenas log e passou a executar processamento real por mensagem
  - Reuso obrigatório do mesmo caso de uso de ingest HTTP (`MessageIngestHandler`) no fluxo assíncrono
  - Validação de mensagens de entrada (`Author`, `Text`, `Timestamp`) antes do processamento
  - Estratégia de retry para falhas transitórias com até 3 tentativas
  - Tratamento explícito de duplicata no fluxo assíncrono usando a mesma regra de idempotência do ingest

### 🔧 Melhorias Técnicas

- **Resiliência do worker**
  - Loop de reconexão do `ChatWorker` ajustado para `Task.Run` assíncrono
  - Backoff de reconexão com `await Task.Delay(...)` respeitando `CancellationToken`
- **Observabilidade mínima por mensagem no worker**
  - Log estruturado com `IdempotencyKey`, `Status`, `Error`, `Provider` e `DateTime`

### 🧪 Testes

- Expansão de cobertura em `ChatProcessorServiceTests`
  - processamento de mensagem válida reutilizando ingest
  - descarte de mensagem inválida sem acionar repositórios/adapter
  - retry em falha transitória e continuidade do consumo
  - interrupção imediata em mensagem duplicada sem dispatch/retry adicional
  - atualização de fila no fluxo assíncrono quando mensagem atende `QueuePolicy` (`!fila`)
  - cancelamento antes da leitura do canal
- Ajuste de `ChatWorkerTests` para novo construtor com `IServiceScopeFactory`
- Execução validada dos testes focados de worker/ingest sem falhas (`ChatProcessorServiceTests`: 6/6)

### 📚 Documentação

- Mini-spec 05 movida para concluída (`docs/specs/done/05-processamento-real-chat-worker.md`)
- Spec 11 atualizada para segurança por token em endpoints protegidos, com exceção explícita de `POST /auth/login` e `POST /auth/register`
- Inclusão das novas mini-specs planejadas:
  - Spec 15: tabela de logs com auditoria mínima
  - Spec 16: auditoria de origem de inserção em `ChatMessages`
- `docs/SPEC.md` e `docs/specs/README.md` sincronizados com status, prioridades e decisões mais recentes

## [v0.5.0] - 2026-06-24

### ✨ Funcionalidades

- **Registro de conta** (Spec 12) com `POST /auth/register`
  - Criação de usuário com validação de e-mail e confirmação obrigatória de senha (`confirmPassword`)
  - Bloqueio de e-mail duplicado com retorno `409 Conflict`
  - Persistência de senha somente em hash (PBKDF2) sem exposição de dados sensíveis
  - Resposta `201 Created` com envelope `Result<T>`

### ⚙️ Configuração

- Política de senha externalizada para `appsettings.json` via seção `PasswordPolicy`
  - `MinLength`
  - `RequireUppercase`
  - `RequireLowercase`
  - `RequireDigit`
  - `RequireSpecialCharacter`
- `RegisterHandler` passou a usar `PasswordValidator` com `PasswordPolicy` injetada por DI

### 🔧 Melhorias Técnicas

- Contrato HTTP de `Result<T>` reforçado em todos os status dos endpoints de Auth e Messages:
  - `ResultExtensions` agora retorna envelope completo também em respostas de sucesso (`200`/`201`)
  - Respostas `401` e `403` passam a incluir payload padronizado de erro com `Result<T>`
  - Metadados `Produces(...)` alinhados ao envelope em todos os códigos documentados

### 🧪 Testes

- `RegisterHandlerTests` atualizado para cenários reais (sem mock de repositório/hasher)
  - Execução com `UserRepository` real + SQLite em memória (`RepositoryTestDbFactory`)
  - Uso de `PasswordHasher` real e validação por `Verify`
  - Cobertura focada em fluxos reproduzíveis de ponta a ponta no handler

### 📚 Documentação

- Atualização do `SPEC.md` e README para refletir registro de conta e política de senha configurável
- Mini-spec 12 promovida para concluída

## [v0.4.0] - 2026-06-23

### ✨ Funcionalidades

- **Autenticação com validação de senha** (Spec 02) - Login seguro com validação de credenciais
  - `IPasswordHasher` contrato para serviços de hashing de senha
  - `PasswordHasher` implementado com PBKDF2-SHA256, 10.000 iterações e salt aleatório
  - Constant-time comparison para prevenir timing attacks
  - Resposta de erro unificada para não expor se email existe ou não
  - Código HTTP mudou de `404 Not Found` para `401 Unauthorized`

### 🧪 Testes

- Testes de `LoginHandler` expandidos: cenários de senha correta, incorreta e usuário inexistente
- Novos testes unitários em `PasswordHasherTests.cs`:
  - Geração de diferentes hashes para mesma senha (salts aleatórios)
  - Verificação correta de senha com hash correspondente
  - Edge cases: empty/null password/hash
  - Exceptions para entrada inválida

### 🏗️ Mudanças Arquiteturais

- Adição de `IPasswordHasher` à injeção de dependência em `DependencyInjection.cs`
- `LoginHandler` agora injeta `IPasswordHasher` como terceiro parâmetro
- Testes de handlers (`LoginHandlerTests`, `MessageIngestHandlerTests`) migrados de fakes manuais para `Moq` com `It.IsAny` onde aplicável

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

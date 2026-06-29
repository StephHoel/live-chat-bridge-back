# Live Chat Bridge Backend

Backend centralizado em .NET 9 para ingestão e processamento de mensagens de chat de lives, servindo como ponte entre provedores de live chat (ex: TikTok) e uma camada de automação, fila, comando e distribuição.

## 📋 Status do Projeto

**Fase atual:** Prototipal funcional com persistência durável.

- ✅ API REST com autenticação JWT
- ✅ Registro de conta com política de senha configurável
- ✅ Configuração persistida de live por usuário (`GET /config/live`, `PUT /config/live`)
- ✅ Ingestão de mensagens com detecção de comandos (endpoint protegido por JWT)
- ✅ Fila de usuários
- ✅ Worker background para provedores de live (TikTok)
- ✅ Controle do worker por usuário autenticado (`POST /worker/start`, `POST /worker/stop`, `GET /worker/status`)
- ✅ Persistência durável com SQLite/EF Core
- ✅ Processamento real no worker com reuso do caso de uso de ingestão
- 🎯 Diretriz de evolução: suporte a N usuários simultâneos com múltiplos workers concorrentes (um worker lógico por usuário)
- 📋 Evolução guiada por mini-specs (`docs/specs/planned`, `active`, `done`)

## 🚀 Quick Start

### Pré-requisitos

- .NET 9 SDK
- Visual Studio 2022 ou VS Code com C# DevKit

### Build & Run

```bash
# Build da solução
dotnet build LCB.sln

# Executar testes
dotnet test LCB.sln

# Executar API (porta padrão: 5000)
dotnet run --project src/LCB.Api/LCB.Api.csproj
```

## 📁 Estrutura

```plain
src/
  LCB.Api/            - ASP.NET Core Minimal API, endpoints, middleware
  LCB.Application/    - Handlers de caso de uso, workers, serviços
  LCB.Domain/         - Entidades, DTOs, enums, contratos
  LCB.Infrastructure/ - Repositórios EF Core, provedores externos
test/
  LCB.UnitTest/       - Testes unitários
  LCB.IntegrationTest/ - Testes de integração dos endpoints HTTP
docs/
  SPEC.md             - Especificação viva do projeto
  specs/              - Mini-specs organizadas por status (planned/active/done)
```

## 🔌 Endpoints Principais

### Autenticação

- `POST /auth/login` - Login por email com validação de senha
- `POST /auth/register` - Registro de conta com validações de email e senha

### Mensagens

- `POST /messages/ingest` - Ingestão de mensagens com detecção de comandos (requer token JWT)

### Configuração de Live

- `GET /config/live` - Consulta configuração de live do usuário autenticado (auto-cria configuração padrão se não existir)
- `PUT /config/live` - Atualiza parcialmente usernames/reload da configuração de live do usuário autenticado

### Controle de Worker

- `POST /worker/start` - Inicia listeners para a instância do usuário autenticado
- `POST /worker/stop` - Interrompe listeners da instância do usuário autenticado
- `GET /worker/status` - Retorna estado atual do worker do usuário autenticado

### Contrato de resposta

- Todos os endpoints retornam envelope `Result<T>` tanto em sucesso quanto em erro.
- Os metadados HTTP (`Produces`) devem refletir o contrato `Result<T>` em todos os status codes.

## 🏗️ Arquitetura

- **Padrão:** Arquitetura em camadas (API → Application → Domain ← Infrastructure)
- **Persistência:** EF Core com SQLite (desenvolvimento), PostgreSQL (roadmap)
- **Async:** `System.Threading.Channels` para comunicação entre worker e processador
- **Testes:** xUnit com padrão de fábrica para fixtures
- **Logging:** Logger customizado com middleware de correlação por request

## ⚙️ Configuração

- `JWT_KEY`: chave usada para assinatura de JWT
- `ConnectionStrings:DefaultConnection`: conexão do banco SQLite
- `Usernames`: configurações de usernames de live (`Tiktok`, `Twitch`, `Youtube`)
- `LiveSettings` (banco): configuração operacional persistida por usuário (usernames por plataforma + `ReloadTimeInSec`)
- `PasswordPolicy`: política de senha para registro
  - `MinLength`
  - `RequireUppercase`
  - `RequireLowercase`
  - `RequireDigit`
  - `RequireSpecialCharacter`

## 📚 Documentação

- [SPEC.md](docs/SPEC.md) - Guia completo do produto e decisões de arquitetura
- [CHANGELOG.md](docs/CHANGELOG.md) - Histórico de versões e funcionalidades
- [specs/](docs/specs/) - Mini-specs técnicas por status

## 🧪 Cobertura de Testes

- Referência atual da suíte completa (2026-06-29):
  - Comando: `dotnet test LCB.sln -v minimal`
  - Total: 133
  - Falhas: 0

- Referência atual de unit tests com cobertura (2026-06-29):
  - Comando: `dotnet test test/LCB.UnitTest/LCB.UnitTest.csproj --configuration Release --collect:"XPlat Code Coverage;Format=cobertura" --results-directory ./TestResults -v minimal`
  - Total: 105
  - Falhas: 0
  - Cobertura de linhas: **90,82%** (`line-rate=0.9082`)

## 🤝 Para Contribuidores

Antes de qualquer mudança:

1. **Leia [docs/SPEC.md](docs/SPEC.md)** - É a fonte de verdade do projeto
2. **Verifique specs em [docs/specs/](docs/specs/)** - Seguimos process de spec-driven
3. **Consulte a branch ativa** - Verifique se há PR aberta com features em progresso
4. **Rodando testes** - Todas as mudanças devem passar em `dotnet test`
5. **Ao iniciar uma spec** - Atualize o status para `em andamento`, mova de `docs/specs/planned/` para `docs/specs/active/` e atualize [docs/specs/README.md](docs/specs/README.md)
6. **Ao abrir PR de spec implementada** - Atualize o status para `implementado`, mova de `docs/specs/active/` para `docs/specs/done/` e atualize [docs/specs/README.md](docs/specs/README.md)

Mais detalhes em [.github/copilot-instructions.md](.github/copilot-instructions.md).

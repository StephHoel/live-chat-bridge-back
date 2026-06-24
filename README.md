# Live Chat Bridge Backend

Backend centralizado em .NET 9 para ingestão e processamento de mensagens de chat de lives, servindo como ponte entre provedores de live chat (ex: TikTok) e uma camada de automação, fila, comando e distribuição.

## 📋 Status do Projeto

**Fase atual:** Prototipal funcional com persistência durável.

- ✅ API REST com autenticação JWT
- ✅ Registro de conta com política de senha configurável
- ✅ Ingestão de mensagens com detecção de comandos
- ✅ Fila de usuários
- ✅ Worker background para provedores de live (TikTok)
- ✅ Persistência durável com SQLite/EF Core
- 🔄 Lógica de processamento em desenvolvimento
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
dotnet test test/LCB.UnitTest/LCB.UnitTest.csproj

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
docs/
  SPEC.md             - Especificação viva do projeto
  specs/              - Mini-specs organizadas por status (planned/active/done)
```

## 🔌 Endpoints Principais

### Autenticação

- `POST /auth/login` - Login por email com validação de senha
- `POST /auth/register` - Registro de conta com validações de email e senha

### Mensagens

- `POST /messages/ingest` - Ingestão de mensagens com detecção de comandos

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

## 🤝 Para Contribuidores

Antes de qualquer mudança:

1. **Leia [docs/SPEC.md](docs/SPEC.md)** - É a fonte de verdade do projeto
2. **Verifique specs em [docs/specs/](docs/specs/)** - Seguimos process de spec-driven
3. **Consulte a branch ativa** - Verifique se há PR aberta com features em progresso
4. **Rodando testes** - Todas as mudanças devem passar em `dotnet test`

Mais detalhes em [.github/copilot-instructions.md](.github/copilot-instructions.md).

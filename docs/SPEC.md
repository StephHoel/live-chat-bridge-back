# Live Chat Bridge Backend - Spec Driven Guide para IA

> Status: rascunho vivo. Este arquivo deve ser atualizado sempre que uma decisão de produto, arquitetura, design ou processo mudar.
> **Versão do Projeto:** v0.6.6

Este spec orienta futuras interações com ferramentas de IA como Codex, GitHub Copilot, ChatGPT ou agentes similares. Use-o como fonte primária antes de propor código, refatorações, testes, automações ou mudanças de produto.

## 1. Contexto do Produto

O projeto implementa um backend em .NET para centralizar ingestão e processamento de mensagens de chat de lives.

Hoje existem dois eixos principais no produto:

- API HTTP para autenticação e ingestão manual/programática de mensagens;
- worker em background para captar eventos de live do TikTok e encaminhá-los para processamento interno.

O objetivo é servir como ponte entre provedores de live chat e uma camada de automação, fila, comando e distribuição para frontend.

O sistema ainda está em fase inicial/prototipal: já possui persistência local durável via SQLite/EF Core, comandos de chat simples, autenticação JWT com endpoints protegidos e processamento assíncrono real no worker.

## 2. Funcionalidades Existentes

- Login por e-mail via `POST /auth/login`, com validação de senha e emissão de JWT.
- Registro de conta via `POST /auth/register`, com validação de e-mail, confirmação de senha e política configurável.
- Configuração operacional por usuário via `GET /config/live` e `PUT /config/live`, com persistência durável de usernames por plataforma e `ReloadTimeInSec`.
- Controle operacional de worker por usuário autenticado via `POST /worker/start`, `POST /worker/stop` e `GET /worker/status`.
- Ingestão de mensagens via `POST /messages/ingest` com autenticação JWT obrigatória, convertendo payload HTTP em `LCB.Domain.Entities.ChatMessageEntity`.
- Detecção de comandos no texto da mensagem por `AdapterService`, com dispatch para handlers registrados em `CommandRegistry`.
- Comando `!fila`, que hoje retorna resposta de sucesso simulada pelo `FilaCommandHandler`.
- Comando `!comando`, que hoje retorna resposta de sucesso simulada pelo `TestCommandHandler`.
- Atualização de fila persistida para usuários que enviam mensagens reconhecidas por `ShouldJoinQueue()`.
- **Persistência durável** para `UserEntity`, `QueueEntity` e `ChatMessageEntity` via EF Core com SQLite local (Spec 03 ✅).
- **Persistência durável** para `LiveSettingsEntity` por usuário, incluindo auditoria mínima por e-mail em `UpdatedByUser` (Spec 19 ✅).
- **Fundação da auditoria persistida** (Spec 15 ✅): tabela `AuditLogs`, status tipado por enum persistido como string, serviço de escrita (`IAuditLogService`) e repositório dedicado (`IAuditLogRepository`).
- **Rollout de auditoria operacional** (Spec 23 ✅): catálogo fechado de ações/recursos, contrato `MetadataJson` v1 com validação semântica, política de segunda tentativa na escrita e instrumentação em endpoints operacionais, ingestão HTTP, worker assíncrono e tarefas de retenção.
- **Auditoria de origem de inserção em mensagens** (Spec 16 ✅): `ChatMessageEntity` diferencia `Author` (autor do chat) de `InsertedByUser` (ator que inseriu no backend), com preenchimento por usuário autenticado no fluxo HTTP e pelo usuário autenticado que ativou a sessão no worker.
- **Migrations versionadas** para evolução controlada do schema; índices otimizados e preparação para PostgreSQL.
- Worker hospedado (`ChatWorker`) dedicado ao processamento assíncrono do canal interno.
- Listeners de live (TikTok) iniciados/parados sob demanda por usuário autenticado via endpoints de controle operacional.
- Canal em memória (`System.Threading.Channels`) para receber mensagens do provedor e entregá-las ao `ChatProcessorService`.
- Conversor JSON tolerante para `DateTime` nas entradas HTTP.
- **Logging padronizado** com `TemplateLoggerProvider`, middleware de correlação por request e `OperationExecutor` (Spec 13 ✅).
- Tratamento centralizado de erros com logs estruturados e sem exposição de dados sensíveis.
- **Idempotência de mensagens** (Spec 01 ✅): chave derivada de `Provider + Author + Timestamp` normalizado; reprocessamento de mensagens não processadas via `UpdateAsync`; duplicatas já processadas retornam `400 Duplicate`.
- **Autenticação com validação de senha** (Spec 02 ✅): Login valida `password` contra `PasswordHash` usando PBKDF2-SHA256; resposta unificada `401 Unauthorized` para email/senha inválidos (sem enumeration attacks); implementação em `IPasswordHasher` com constant-time comparison.
- **Segurança por token para endpoints protegidos** (Spec 11 ✅): `POST /auth/login` e `POST /auth/register` permanecem públicos; demais endpoints HTTP usam autenticação no pipeline com `FallbackPolicy` + policy `ProtectedApi`.
- **Exceção de autenticação para Swagger em Development** (Spec 22 ✅): endpoints de documentação (`/swagger/index.html` e `/swagger/v1/swagger.json`) ficam públicos somente em `Development`; endpoints de negócio permanecem protegidos por token.

## 3. Funcionalidades Planejadas

As mini-specs ficam em `docs/specs/` e são organizadas por status em `planned/`, `active/`, `done/` e `discontinued/`. A estrutura já existe no repositório. Consulte a mini-spec correspondente antes da implementação.

Specs em `docs/specs/done/` representam histórico implementado e não devem ter seu conteúdo original alterado. Quando necessário, apenas inclua complementos (adendos/contexto) sem reescrever decisões já registradas.

### Diretriz transversal para escalabilidade de workers

- O sistema deve estar apto a operar com **N usuários conectados simultaneamente**.
- O backend deve suportar **execução concorrente de múltiplos workers/listeners**, mantendo isolamento por usuário (um worker lógico por usuário/sessão ativa).
- Novas mini-specs e implementações não podem assumir worker único global como premissa fixa.

Antes de implementar qualquer item planejado, a IA deve pedir ou propor uma mini-spec no formato da seção 16 deste documento.

**Fallback enquanto não houver mini-spec formal:** se a pasta `docs/specs/planned/` não contiver um arquivo para a funcionalidade solicitada, a IA deve propor um rascunho de mini-spec ao usuário antes de escrever qualquer código, e aguardar confirmação.

### Status Atual de Planejamento

- **Planejadas:** 8 specs em `docs/specs/planned/`
- **Ativas:** 0 specs em `docs/specs/active/`
- **Concluídas:** 14 specs em `docs/specs/done/`
- **Descontinuadas:** 1 spec em `docs/specs/discontinued/`

### Próximas Prioridades Sugeridas

1. **Spec 21** - Nome de usuário para auditoria operacional
2. **Spec 06** - Endpoint de recuperação de acesso

Apenas o usuário define a ordem de implementação. A IA deve respeitar a priorização dada, mesmo que sugerir uma sequência técnica diferente.

## 4. Stack Real do Projeto

- .NET 9 com solução Visual Studio (`LCB.sln`).
- ASP.NET Core Minimal API no projeto `LCB.Api`.
- Arquitetura em camadas com projetos `Api`, `Application`, `Domain`, `Infrastructure` e `UnitTest`.
- JWT Bearer Authentication com `Microsoft.AspNetCore.Authentication.JwtBearer`.
- Swagger via `Swashbuckle.AspNetCore` apenas em ambiente de desenvolvimento.
- Worker/Hosted Service com `BackgroundService`.
- Comunicação assíncrona interna com `System.Threading.Channels`.
- Integração com TikTok Live por `TikTokLive_Sharp`.
- Testes unitários e de integração com xUnit.
- Persistência atual via EF Core com SQLite local e migrations versionadas.

## 5. Estrutura de Pastas

- `.github`: workflows, Dependabot, assets e instruções para Copilot.
- `docs`: documentação principal do projeto. Contém este spec e a árvore `docs/specs/` com as pastas `planned/`, `active/`, `done/` e `discontinued/` para mini-specs.
- `src/LCB.Api`: entrypoint HTTP, DI, endpoints, middleware, logging e extensões de API.
- `src/LCB.Application`: handlers de caso de uso, configuração, serviços de processamento e workers.
- `src/LCB.Domain`: contratos, entidades, enums, DTOs, objetos de resultado e modelos compartilhados.
- `src/LCB.Infrastructure`: repositórios persistentes EF Core, handlers de comando, provedores externos, serviços concretos e migrations.
- `test/LCB.UnitTest`: testes unitários de handlers, serviços, workers e repositórios persistentes.
- `test/LCB.IntegrationTest`: testes de integração de endpoints HTTP (`/auth/login`, `/auth/register`, `/messages/ingest`, `/config/live`, `/worker/start`, `/worker/stop`, `/worker/status`).

## 6. Fluxos Principais

### Login

1. `AuthEndpoints` recebe `POST /auth/login` com `email` e `password`.
2. `LoginHandler` busca usuário por e-mail em `IUserRepository`.
3. Se não encontrado ou senha inválida, retorna `401 Unauthorized` com mensagem genérica `"Invalid email or password"` (evita enumeration).
4. Se encontrado e senha válida, `JwtTokenService` gera token com `NameIdentifier` e `Email`.
5. Token é retornado em resposta `200 OK`.
6. A resposta é convertida por `ResultExtensions.ToMinimalResult()`.

**Implementação de segurança:**

- Validação de senha via `IPasswordHasher.Verify()` com constant-time comparison
- PBKDF2-SHA256 com 10.000 iterações, 128-bit salt aleatório, 256-bit hash
- Mensagens de erro genéricas para não expor se email existe ou não

### Registro

1. `AuthEndpoints` recebe `POST /auth/register` com `email`, `password` e `confirmPassword`.
2. `RegisterHandler` normaliza e valida e-mail.
3. A senha é validada por `PasswordValidator` com política carregada de `PasswordPolicy` no `appsettings`.
4. `confirmPassword` é obrigatória e deve ser igual a `password`.
5. O handler verifica duplicidade de e-mail em `IUserRepository`.
6. Em sucesso, persiste `UserEntity` com hash de senha (`IPasswordHasher`) e retorna `201 Created`.
7. A resposta é convertida por `ResultExtensions.ToMinimalResult()` mantendo envelope `Result<T>`.

### Ingestão HTTP de Mensagens

1. `MessageEndpoints` recebe `POST /messages/ingest`.
2. A rota exige usuário autenticado via JWT (`Authorization: Bearer <token>`).
3. `MessageIngestHandler` converte o request para `LCB.Domain.Entities.ChatMessageEntity`.
4. O handler tenta verificar duplicidade por `IdempotencyKey`.
5. Se a mensagem indicar entrada em fila, o usuário é inserido/atualizado em `IQueueRepository`.
6. O texto é parseado e despachado pelo `AdapterService`.
7. A mensagem é marcada como processada e salva em `IMessageRepository`.

### Configuração de Live por Usuário

1. `ConfigEndpoints` recebe `GET /config/live` ou `PUT /config/live`.
2. A rota exige usuário autenticado via JWT e extrai `UserId` e `Email` dos claims.
3. `GET /config/live` busca a configuração do usuário em `ILiveSettingsRepository`.
4. Se a configuração ainda não existir, o backend cria automaticamente um registro com defaults e retorna `200 OK`.
5. `PUT /config/live` opera como atualização parcial: apenas os campos enviados são alterados.
6. Usernames são normalizados com `trim`, remoção de `@` inicial e extração de handle quando informados como URL.
7. Toda atualização registra `UpdatedByUser` com o e-mail do usuário autenticado.

### Worker de Live

1. `ChatWorker` inicia apenas o `ChatProcessorService`, mantendo o consumo do canal interno.
2. O front autenticado aciona `POST /worker/start` para iniciar listeners da própria instância de usuário (não global).
3. `WorkerControlService` valida usernames persistidos por plataforma e inicia/paralisa os listeners selecionados para o usuário autenticado.
4. O provedor escreve mensagens em um `ChannelWriter<LCB.Domain.Models.ChatMessageModel>` (tipo atual do canal, tratado como `WorkerInput`).
5. `ChatProcessorService` consome o `ChannelReader`, converte `WorkerInput` para entidade de domínio e reutiliza `MessageIngestHandler` para processamento real com validação, idempotência e retry.

## 7. Configuração e Ambiente

- `appsettings.json` define `JWT_KEY`, a seção `Usernames` com `Tiktok`, `Twitch` e `Youtube`, e a seção `PasswordPolicy`.
- `appsettings.Development.json` já contém uma chave JWT de desenvolvimento e um username de TikTok preenchido.
- `ConnectionStrings:DefaultConnection` define o banco SQLite local.
- `LiveConfig.SectionName` permanece disponível por compatibilidade de configuração, mas o acionamento de listeners usa usernames persistidos por usuário (`LiveSettings`).
- `PasswordPolicy` define requisitos mínimos de senha (`MinLength`, `RequireUppercase`, `RequireLowercase`, `RequireDigit`, `RequireSpecialCharacter`).
- `AuditRetention` define política de retenção de auditoria (`EndpointOperationalTtlDays`, `WorkerFlowTtlDays`, `SystemTaskTtlDays`, `BatchSize`, `CleanupIntervalHours`, `ReviewThresholdRows`, `ReviewThresholdMb`).
- O Swagger só é exposto em ambiente de desenvolvimento.
- A autenticação JWT depende de `JWT_KEY` com pelo menos 32 bytes; caso contrário, o helper retorna `null`.

## 8. Diretrizes de Código

Ao trabalhar neste projeto, a IA deve:

- preservar a separação atual entre `Api`, `Application`, `Domain` e `Infrastructure`;
- manter contratos no `Domain` e implementações concretas fora dele;
- evitar introduzir dependência direta de infraestrutura dentro de endpoints;
- manter handlers pequenos, com regras de orquestração e retorno por `Result<T>`;
- manter responses de API sempre envelopados em `Result<T>` (sucesso e erro), com metadados de endpoint (`Produces`) refletindo o mesmo contrato;
- preferir mudanças incrementais, porque há partes ainda prototipais e não totalmente consolidadas;
- adicionar ou atualizar testes ao mexer em repositórios, parsing de comandos, autenticação ou idempotência;
- manter consistência de ortografia, acentuação e terminologia em português (pt-BR) em toda documentação do projeto (`docs/`, mini-specs, changelog e instruções), evitando mistura inconsistente de variantes;
- documentar no spec qualquer mudança que altere fluxo de ingestão, autenticação, processamento ou persistência.

## 9. Convenção de Namespaces de Domínio

O projeto divide os tipos de domínio em três categorias com papéis fixos. A IA deve respeitar essa separação ao propor qualquer novo tipo ou modificar tipos existentes, independente do domínio de negócio envolvido.

| Namespace | Papel | Regra |
| --- | --- | --- |
| `LCB.Domain.Entities` | **Modelo de persistência.** Representa a entidade real de negócio; é o único tipo que trafega entre Application e Infrastructure e será mapeado para o banco de dados. | Nunca adaptar para conveniência de API ou de camada interna. Alterações em Entities afetam persistência e devem passar por mini-spec. |
| `LCB.Domain.DTO` | **Transporte interno entre camadas.** Usado por handlers, serviços e workers para trocar dados sem expor a entidade completa. Pode ser ajustado livremente conforme a necessidade de cada caso de uso. | Nunca retornar diretamente em endpoints de API. Não persiste. |
| `LCB.Domain.Models` | **Response de API.** Representa o contrato de saída dos endpoints — é o que o cliente externo recebe. | Nunca usar como modelo de persistência. A conversão de `Entity → Model` deve acontecer nos handlers (`Application`), nunca em `Infrastructure` ou nos endpoints diretamente. |

**Observação sobre `LCB.Domain.Models.ChatMessageModel`:** este tipo, usado no canal assíncrono do worker (`TikTokChatProvider → ChannelWriter → ChatProcessorService`), funciona hoje como transporte interno (equivalente funcional a `WorkerInput` nesse contexto). A conversão para `LCB.Domain.Entities.ChatMessageEntity` deve ocorrer em `ChatProcessorService` por mapeador dedicado (`WorkerInput -> ChatMessageEntity`) antes de qualquer persistência ou lógica de negócio.

### Contratos atuais

- `LCB.Domain.Entities.ChatMessageEntity`: entidade de mensagem; usada em persistência e lógica de negócio no fluxo HTTP.
- `LCB.Domain.Entities.LiveSettingsEntity`: configuração operacional persistida por usuário para usernames de live e `ReloadTimeInSec`.
- `LCB.Domain.Entities.QueueEntity`: entrada de usuário na fila; identidade por `Id` (Guid), indexado por `User`.
- `LCB.Domain.Entities.UserEntity`: usuário autenticável; `Email` como identificador único, `PasswordHash` para validação de senha (armazenado como PBKDF2 hash).
- `LCB.Domain.Interfaces.Services.IPasswordHasher`: contrato para serviços de hashing seguro de senha com métodos `Hash(string)` e `Verify(string, string)`.
- `LCB.Domain.Objects.Result<T>`: envelope padrão para retorno de handlers; sempre use `Result<T>.Ok()` ou `Result<T>.Fail()` — nunca lance exceção para erros de negócio esperados.
- `LCB.Domain.DTO.CommandDTO` / `ParsedCommandDTO`: transporte de resultado de comando e de comando parseado, respectivamente.

## 10. Limitações e Riscos Conhecidos

### Status de Specs Completadas

- ✅ **Spec 05 - Processamento real do worker** (feita): `ChatProcessorService` reutiliza o caso de uso de ingest HTTP, com validação, idempotência, resiliência e observabilidade mínima por mensagem.
- ✅ **Spec 11 - Segurança por token** (feita): policy de autenticação aplicada aos endpoints protegidos, com exceção explícita para `POST /auth/login` e `POST /auth/register`.
- ✅ **Spec 03 - Persistência durável** (feita): Repositórios EF Core com SQLite, migrations versionadas, índices otimizados.
- ✅ **Spec 13 - Observabilidade e tratamento de erros** (feita): Logging centralizado, `OperationExecutor`, remoção de `Console.WriteLine` em componentes críticos.
- ✅ **Spec 12 - Registro de conta** (feita): Endpoint `POST /auth/register` com política de senha configurável, confirmação obrigatória e prevenção de e-mail duplicado.
- ✅ **Spec 04 - Consolidação de modelo de mensagem** (feita): tipo de canal explicitado como `ChatMessageModel` (`WorkerInput`), mapeador dedicado `WorkerInput -> ChatMessageEntity`, normalização temporal unificada em UTC-3 e resposta de ingestão migrada para `Model` de API.
- ✅ **Spec 16 - Campo de auditoria de origem de inserção em ChatMessages** (feita): inclusão de `InsertedByUser` em persistência com fallback legado em migration, preenchimento por e-mail autenticado no fluxo HTTP e pelo usuário autenticado que ativou a sessão no fluxo assíncrono.
- ✅ **Ajuste transversal de timezone (pós-Spec 04)**: entidades de domínio relacionadas (`ChatMessageEntity`, `QueueEntity`, `UserEntity`) e serialização JSON de `DateTime` padronizadas para UTC-3.

### Idempotência (implementada — Spec 01 ✅)

`IdempotencyKey` é derivada de `Provider + Author + Timestamp` (UTC-3, formato ISO 8601). O campo `Timestamp` em `ChatMessageEntity` é `DateTime`; quando ausente no payload, o mapper usa horário atual normalizado para UTC-3.

- Mensagem com mesma chave e `Processed == true`: retorna `400 Bad Request` com `StatusResultEnum.Duplicate`.
- Mensagem com mesma chave e `Processed == false`: reprocessada com `UpdateAsync` (nova tentativa permitida).
- `Author` é normalizado com trim antes do cálculo; `Timestamp` é convertido para UTC-3 quando necessário.

### Autenticação

- Login valida senha com `IPasswordHasher.Verify()` e retorna `401 Unauthorized` para credenciais inválidas.
- Registro de conta retorna `409 Conflict` para e-mail duplicado e `400 Bad Request` para payload inválido.

### Persistência

- Persistência local agora usa SQLite em arquivo `.db` com schema gerenciado por migrations.
- A tabela `LiveSettings` mantém uma linha por usuário, com `UserId` único, usernames por plataforma, `ReloadTimeInSec` e auditoria mínima em `UpdatedByUser`.
- Estratégia de banco online (PostgreSQL) permanece planejada para Spec 14.
- Ainda não há mecanismo de backup, replay ou snapshot.

### Processamento

- `ChatProcessorService` implementa processamento real com reuso do caso de uso de ingest HTTP.
- Consolidação de modelo entre fluxo HTTP e worker implementada na Spec 04, com conversões explícitas por camada e contrato de API sem exposição de entidade de persistência.
- Semântica de entrega no canal interno permanece `at-least-once` intra-processo (sem garantia cross-restart no canal em memória atual).
- O desenho atual ainda evolui para atender plenamente operação com múltiplos workers simultâneos por usuário conectado (escala horizontal por sessão de live).

## 11. Testes e Cobertura Atual

- Há cobertura unitária para handlers de login/ingestão, serviços de autenticação, workers e repositórios persistentes.
- Há cobertura de integração para endpoints de autenticação e ingestão (`/auth/login`, `/auth/register`, `/messages/ingest`), incluindo cenários com token ausente, token inválido e token válido.
- Há cobertura de integração para endpoints de documentação do Swagger (`/swagger/index.html` e `/swagger/v1/swagger.json`) sem token em ambiente `Development`.
- Há cobertura de integração para `GET /config/live` e `PUT /config/live`, incluindo autenticação obrigatória, auto-provisionamento, atualização parcial e validação de `ReloadTimeInSec`.
- Há cobertura de integração para `POST /worker/start`, `POST /worker/stop` e `GET /worker/status`, incluindo autenticação obrigatória, transições de estado e isolamento por usuário autenticado.
- Há cobertura para `RegisterHandler` incluindo sucesso, validações de payload, conflito por duplicidade e persistência de hash.
- Repositórios EF (`UserRepository`, `QueueRepository`, `ChatMessageRepository`, `LiveSettingsRepository`) possuem testes com SQLite em memória.
- Repositório de auditoria (`AuditLogRepository`) possui testes com SQLite em memória.
- Há cobertura de `RepositoryBase` para fluxos de sucesso e erro.
- Serviço de auditoria (`AuditLogService`) possui testes de validação para JSON inválido e bloqueio de conteúdo sensível em metadata.
- Há cobertura unitária para normalização de usernames e handlers de leitura/atualização da configuração de live.
- Testes unitários para geração estável de `IdempotencyKey` em `ChatMessageEntityTests`.
- Testes de handler cobrem: mensagem nova, duplicata processada, reprocessamento (`Processed == false`), falha de persistência e erro de adapter.
- Execução de referência da solução completa (2026-07-02): `dotnet test LCB.sln -v minimal` com 144 testes aprovados e 0 falhas.
- Execução de referência de unit tests (2026-07-02): `dotnet test test/LCB.UnitTest/LCB.UnitTest.csproj -v minimal` com 123 testes aprovados e 0 falhas.
- Última execução de unit tests com cobertura (2026-07-02): `dotnet test test/LCB.UnitTest/LCB.UnitTest.csproj --collect:"XPlat Code Coverage;Format=cobertura" --results-directory ./TestResults -v minimal` com 123 testes aprovados, 0 falhas e cobertura de linhas em 81,92% (Cobertura `line-rate=0.8192`).

## 12. Convenções Observadas

- Injeção de dependência concentrada em métodos de extensão `Add...`.
- Endpoints minimalistas delegando a handlers de aplicação.
- Uso de `Result<T>` para padronizar retornos de sucesso/erro.
- Logging com mensagens de início/fim de método em vários componentes.
- Repositórios usam `RepositoryBase` para padronizar logging e tratamento de exceções.

## 13. Pendências de Documentação

- A estrutura `docs/specs/planned/`, `docs/specs/active/`, `docs/specs/done/` e `docs/specs/discontinued/` já existe no repositório.
- Manter mini-specs planejadas atualizadas com mudanças de contrato, incluindo em qual spec já implementada está a versão anterior do contrato.
- Atualizar README quando o fluxo de autenticação ou de ingestão mudar de forma material.

## 14. Prioridades Técnicas Evidentes

A ordem de implementação é sempre definida pelo usuário. A IA pode sugerir uma sequência, mas nunca deve impô-la ou assumir que a ordem abaixo é mandatória.

- concluir mitigação de durabilidade/replay no worker com persistência funcional de inbox (Spec 17).
- evoluir auditoria operacional para nome de usuário (Spec 21).

## 15. Como a IA Deve Trabalhar Neste Projeto

### Antes de qualquer ação (codificação, criação de mini-spec, refatoração ou documentação)

1. **reler `docs/SPEC.md`** para verificar se houve alterações feitas pelo usuário desde a última leitura;
2. verificar os arquivos diretamente envolvidos;
3. confirmar se a solicitação muda produto, persistência ou dependências;
4. propor uma mini-spec quando o pedido estiver ambíguo ou afetar comportamento de usuário.

### Antes de criar uma mini-spec, adicionalmente

1. listar todos os arquivos existentes em `docs/specs/planned/`, `docs/specs/active/`, `docs/specs/done/` e `docs/specs/discontinued/`;
2. ler o conteúdo de cada mini-spec existente e verificar se a nova proposta interfere em alguma delas (sobreposição de escopo, dependência implícita, conflito de comportamento ou alteração de contrato compartilhado);
3. se houver qualquer interferência, descrever o conflito ao usuário e aguardar instrução explícita antes de criar o arquivo;
4. nunca determinar a ordem de implementação de mini-specs — a numeração indica apenas sequência de criação. A IA pode sugerir uma ordem ao usuário, mas a decisão final é sempre do usuário.

### Durante a implementação

- seguir os padrões existentes de pasta, nomes e componentes;
- manter alterações pequenas e rastreáveis;
- não refatorar código não relacionado;
- proteger dados persistidos;
- preservar comportamento existente por padrão;

### Fluxo obrigatório de status das mini-specs

- ao iniciar uma mini-spec, atualizar o status para **em andamento**, mover o arquivo de `docs/specs/planned/` para `docs/specs/active/` e atualizar `docs/specs/README.md` para listá-la em **Ativas/Em andamento**;
- ao abrir PR com a mini-spec implementada, atualizar o status para **implementado**, mover o arquivo de `docs/specs/active/` para `docs/specs/done/` e atualizar `docs/specs/README.md` para listá-la no final da lista de **Implementadas**.
- pedir confirmação quando uma mudança alterar comportamento de usuário, formato de dados, formato de compartilhamento ou compatibilidade;
- atualizar este spec quando uma decisão nova for tomada;
- atualizar README somente quando a informação for útil para usuários ou contribuidores.

### Ao finalizar

- descrever arquivos alterados;
- informar verificações executadas;
- declarar riscos ou pendências;
- apontar decisões que ainda precisam de confirmação humana.

## 16. Template de Mini-Spec para Novas Features

As mini-specs do projeto ficam em `docs/specs/` e são separadas por status. Novas mini-specs devem nascer em `docs/specs/planned/`, migrar para `docs/specs/active/` quando entrarem em execução e ir para `docs/specs/done/` quando virarem referência estável. Mini-specs canceladas devem ser movidas para `docs/specs/discontinued/`. Elas devem ser escritas em pt-BR, incluindo acentuação e caracteres especiais.

**Protocolo obrigatório antes de criar uma mini-spec** (ver também segundo bloco da seção 15):

1. Reler `docs/SPEC.md` para garantir que reflete o estado atual do projeto.
2. Listar e ler todas as mini-specs existentes em `planned/`, `active/`, `done/` e `discontinued/`.
3. Identificar se a nova mini-spec interfere em qualquer mini-spec existente.
4. Se houver interferência de qualquer tipo, apresentar o conflito ao usuário e aguardar definição antes de criar o arquivo.

Convenção obrigatória de mini-spec:

- nome de arquivo com prefixo numérico sequencial **de criação** (não de implementação): `NN-nome-da-feature.md`;
- a numeração indica apenas a ordem em que a mini-spec foi criada — **a ordem de implementação é sempre definida pelo usuário**;
- campo `Número: NN` logo após o título;
- campo `Status:` mantido e atualizado conforme o estágio (`planejado`, `ativa`, `implementado`, `concluída`, `descontinuado`).

Copie e preencha o modelo abaixo antes de implementar funcionalidades maiores que ainda não tenham documento próprio:

```md
# Mini-spec: <nome>

Número: <NN>
Status: planejado

## Problema
- <qual dor ou lacuna técnica/funcional resolve?>

## Comportamento esperado
- <o que deve acontecer quando implementado?>
- <incluir casos de sucesso e de erro relevantes>

## Superfícies afetadas
- Endpoints: <rotas HTTP afetadas, ex: POST /messages/ingest>
- Handlers: <handlers de aplicação afetados>
- Workers/Provedores: <workers ou providers afetados, se houver>
- Integrações externas: <serviços externos envolvidos, se houver>

## Dados e persistência
- <campos novos ou alterados nas entidades>
- <impacto nos repositórios persistentes atuais e na futura migração de provider>
- <compatibilidade com dados existentes>

## Contratos de API
- Request: <campos e tipos esperados na entrada>
- Response: <campos e tipos esperados na saída>
- Códigos HTTP: <códigos de resposta esperados para cada caso>

## Regras de validação
- <entradas válidas e inválidas>
- <regras de negócio que devem ser aplicadas antes de persistir>

## Critérios de aceite
- <lista objetiva e testável para validar a implementação>

## Testes esperados
- <casos de teste unitário e/ou de integração que devem existir ao final>

## Fora de escopo
- <o que não será feito nesta mini-spec>
```

## 17. Decisões Registradas

- O projeto está documentado como backend de ponte para chats de live, com foco atual em TikTok e ingestão HTTP.
- O estado atual deve ser tratado como protótipo funcional, não como baseline estável de produção.
- Idempotência, persistência e consolidação do modelo de mensagem são temas estruturais e devem passar por mini-spec antes de mudanças maiores.
- Para a Spec 05, o fluxo do worker deve reutilizar o mesmo caso de uso do ingest HTTP (direto ou via serviço compartilhado), evitando duplicação de regra de negócio.
- Para providers sem ID nativo de mensagem, a idempotência no fluxo assíncrono deve usar `Provider + Author + Timestamp` do provider (normalizado para UTC-3).
- A semântica de entrega do canal interno na Spec 05 é `at-least-once` intra-processo; não há garantia cross-restart com o canal em memória atual.
- Observabilidade mínima por mensagem na Spec 05: `IdempotencyKey`, `Status`, `Error` (quando houver), `Provider` e `DateTime`.
- Para segurança de acesso da Spec 11, `POST /auth/login` e `POST /auth/register` são rotas públicas; demais endpoints protegidos exigem token.
- A validação de token da Spec 11 deve ser centralizada no pipeline HTTP, evitando duplicação em endpoints/handlers.

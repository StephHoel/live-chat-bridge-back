# Fluxos de Funcionamento do Sistema

Documento de consolidação dos fluxos atualmente implementados no backend Live Chat Bridge, com base no estado real do código e dos testes.

## Escopo da análise

- API HTTP (autenticação, configuração, controle de worker e ingestão)
- Pipeline de autorização
- Processamento assíncrono do worker
- Regras transversais de idempotência, fila, comando e persistência

## Visão geral dos fluxos ativos

Atualmente o sistema opera com 6 fluxos principais:

1. Registro de conta
2. Login e emissão de JWT
3. Configuração de live por usuário autenticado
4. Controle operacional do worker por usuário autenticado
5. Ingestão HTTP autenticada de mensagem
6. Processamento assíncrono de mensagens de live (channel -> ingest)

## Fluxo 1 - Registro de conta

Objetivo:

Criar usuário autenticável com e-mail único e senha validada por política.

Entrada:

- Endpoint: POST /auth/register
- Dados: email, password, confirmPassword

Processamento:

1. Endpoint público recebe requisição.
2. Handler normaliza e valida e-mail.
3. Handler valida senha com PasswordValidator (política em configuração).
4. Handler exige confirmação de senha e igualdade com password.
5. Handler verifica duplicidade por e-mail.
6. Em sucesso, gera hash de senha e persiste usuário.
7. Em condição de corrida de duplicidade, retorna conflito.

Saídas esperadas:

- 201 Created: conta criada
- 400 Bad Request: payload inválido
- 409 Conflict: e-mail já cadastrado
- 500 Internal Server Error: falha inesperada de persistência

Arquivos base:

- src/LCB.Api/Endpoints/AuthEndpoints.cs
- src/LCB.Application/Commands/Register/RegisterHandler.cs

## Fluxo 2 - Login e emissão de JWT

Objetivo:

Autenticar usuário por credenciais e fornecer token para rotas protegidas.

Entrada:

- Endpoint: POST /auth/login
- Dados: email, password

Processamento:

1. Endpoint público recebe requisição.
2. Handler busca usuário por e-mail.
3. Se usuário não existir, retorna erro genérico de autenticação.
4. Se a senha não confere com o hash, retorna erro genérico de autenticação.
5. Se as credenciais forem válidas, gera JWT.
6. Retorna token no envelope padrão Result<T>.

Saídas esperadas:

- 200 OK: token emitido
- 401 Unauthorized: credenciais inválidas
- 500 Internal Server Error: falha ao gerar token

Arquivos base:

- src/LCB.Api/Endpoints/AuthEndpoints.cs
- src/LCB.Application/Commands/Login/LoginHandler.cs

## Fluxo 3 - Configuração de live por usuário autenticado

Objetivo:

Persistir e consultar usernames operacionais por usuário para uso no start seletivo de listeners.

Entrada:

- Endpoints: GET /config/live e PUT /config/live
- Header: Authorization Bearer token_jwt

Processamento:

1. Endpoints exigem policy ProtectedApi.
2. O backend extrai UserId e Email do token autenticado.
3. GET retorna configuração existente ou auto-provisiona registro padrão.
4. PUT aplica atualização parcial dos campos enviados.
5. Usernames são normalizados (trim, remoção de @ e extração de handle em URL).
6. Atualizações registram UpdatedByUser com e-mail autenticado.

Saídas esperadas:

- 200 OK: leitura/atualização realizada
- 400 Bad Request: payload inválido
- 401 Unauthorized: token ausente/inválido
- 500 Internal Server Error: falha inesperada

Arquivos base:

- src/LCB.Api/Endpoints/ConfigEndpoints.cs
- src/LCB.Application/Commands/Config/Live/Get/GetLiveConfigHandler.cs
- src/LCB.Application/Commands/Config/Live/Put/PutLiveConfigHandler.cs

## Fluxo 4 - Controle operacional do worker por usuário autenticado

Objetivo:

Iniciar, parar e consultar listeners de live por usuário autenticado, sem efeito global em outras sessões.

Entrada:

- Endpoints: POST /worker/start, POST /worker/stop, GET /worker/status
- Header: Authorization Bearer token_jwt
- Request de start: flags de plataforma (tiktok, twitch, youtube)

Processamento:

1. Endpoints exigem policy ProtectedApi.
2. O backend deriva usuário-alvo exclusivamente do token.
3. Start valida payload (ao menos uma plataforma ativa).
4. Start valida configuração persistida para plataformas selecionadas.
5. Sessões de worker são mantidas em memória com isolamento por UserId.
6. Stop encerra a sessão do usuário autenticado e retorna estado atualizado.
7. Status retorna estado da sessão do usuário autenticado.

Saídas esperadas:

- 200 OK: comando aplicado ou estado consultado
- 400 Bad Request: payload inválido (ex.: nenhuma plataforma ativa)
- 401 Unauthorized: token ausente/inválido
- 409 Conflict: configuração faltante para listener selecionado
- 503 Service Unavailable: listener indisponível no runtime atual
- 500 Internal Server Error: falha inesperada

Arquivos base:

- src/LCB.Api/Endpoints/WorkerEndpoints.cs
- src/LCB.Application/Services/WorkerControlService.cs
- src/LCB.Application/Commands/Worker/Start/StartWorkerHandler.cs
- src/LCB.Application/Commands/Worker/Stop/StopWorkerHandler.cs
- src/LCB.Application/Commands/Worker/Get/GetWorkerStatusHandler.cs

## Fluxo 5 - Ingestão HTTP autenticada de mensagem

Objetivo:

Receber mensagem externa, aplicar idempotência, atualizar fila quando aplicável, executar comando e persistir resultado.

Entrada:

- Endpoint: POST /messages/ingest
- Header: Authorization Bearer token_jwt
- Dados: provider, author, text, timestamp

Processamento:

1. Endpoint exige policy ProtectedApi.
2. Pipeline valida token JWT (fallback policy também exige autenticação).
3. Handler mapeia request para entidade de domínio.
4. Handler verifica duplicidade por IdempotencyKey.
5. Se a duplicata já estiver processada, interrompe com status Duplicate.
6. Se mensagem indicar entrada em fila, atualiza ou inclui registro em Queue.
7. Adapter parseia texto e tenta despachar comando registrado.
8. Mensagem é marcada como processada e persistida.

Saídas esperadas:

- 200 OK: mensagem processada (ou erro interno encapsulado no status da resposta)
- 400 Bad Request: duplicata já processada (Status = Duplicate)
- 401 Unauthorized: token ausente ou inválido
- 500 Internal Server Error: erro inesperado no pipeline

Regras importantes:

- IdempotencyKey derivada de Provider + Author + Timestamp normalizado.
- QueuePolicy atual:
  - TikTok: fila ou !fila
  - Twitch: !fila
  - YouTube: /fila ou !fila
- Comandos conhecidos no despachador:
  - !fila
  - !comando

Arquivos base:

- src/LCB.Api/Endpoints/MessageEndpoint.cs
- src/LCB.Api/DependencyInjection/AuthorizationDependencies.cs
- src/LCB.Application/Commands/Message/Ingest/MessageIngestHandler.cs
- src/LCB.Infrastructure/Policies/QueuePolicy.cs
- src/LCB.Infrastructure/Services/Adapter/AdapterService.cs

## Fluxo 6 - Processamento assíncrono de mensagens de live

Objetivo:

Consumir mensagens de live (TikTok) e reaproveitar exatamente o caso de uso de ingestão para manter regras unificadas.

Entrada:

- Origem: TikTokLive_Sharp via TikTokChatProvider
- Transporte interno: ChannelReader/ChannelWriter de ChatMessageModel

Processamento:

1. ChatWorker inicia apenas o processamento do canal interno.
2. WorkerControlService inicia e paralisa listeners sob demanda por usuário autenticado.
3. Provider escreve mensagens no channel em memória.
4. ChatProcessorService lê mensagens continuamente.
5. Mapper converte WorkerInput em ChatMessageEntity.
6. Service valida author, text e timestamp.
7. Service executa MessageIngestHandler em escopo DI por mensagem.
8. Em falhas não duplicadas, aplica retry até 3 tentativas com backoff.
9. Em status Processed ou Duplicate, encerra ciclo da mensagem.
10. O fluxo assíncrono preenche `InsertedByUser` com o usuário autenticado que ativou a sessão do worker.

Saídas esperadas:

- Processamento real com os mesmos efeitos do fluxo HTTP (idempotência, fila, comando, persistência)
- Logs estruturados por mensagem com status e erro
- Reconexão automática do provider em caso de falha no loop do listener

Arquivos base:

- src/LCB.Application/Workers/ChatWorker.cs
- src/LCB.Application/Services/ChatProcessorService.cs
- src/LCB.Application/Services/WorkerInputMapper.cs

## Regras transversais de funcionamento

### Contrato de resposta

- Endpoints retornam envelope Result<T> em sucesso e erro.
- Conversão para status HTTP ocorre em ResultExtensions.

### Autorização

- Rotas públicas: /auth/login e /auth/register.
- Rotas protegidas: /messages/ingest, /config/live e /worker/* (além do fallback para demais rotas não anônimas).

### Persistência

- Banco atual: SQLite com EF Core e migrations.
- Entidades principais persistidas: Users, Queues, ChatMessages e LiveSettings.

### Observabilidade

- CorrelationId middleware e logging padronizado.
- OperationExecutor centraliza início, fim e falha de operações.

### Auditoria operacional persistida

- Auditoria operacional implementada com `AuditLogs` e catálogo canônico de eventos.
- Fluxos instrumentados: worker control, config/live, ingestão HTTP, fluxo assíncrono do worker e tarefas de retenção.
- Escrita de auditoria com segunda tentativa imediata e fallback em log estruturado na dupla falha.
- Retenção ativa por categoria com purge diário em lotes.

## Fluxos planejados (ainda não ativos)

Os seguintes fluxos existem como planejamento e não estão implementados no comportamento atual:

- Mitigação de durabilidade com replay (spec 17)

Diretriz transversal de evolução:

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- O backend deve suportar múltiplos workers/listeners em paralelo, com isolamento por usuário (um worker lógico por usuário ou sessão ativa).

Referências:

- docs/specs/planned/17-mitigacao-durabilidade-worker-replay-e-auditoria.md
- docs/specs/done/23-rollout-de-auditoria-operacional-no-projeto.md

## Evidências de validação

- Testes de integração cobrem auth, config/live, worker/start-stop-status e ingestão (incluindo token ausente/inválido/válido, duplicata, transição de estado e isolamento por usuário).
- Execução de referência registrada no projeto: dotnet test LCB.sln -v minimal com 144 testes aprovados e 0 falhas.

## Planejamento Geral de Sistemas

- Sistema de Registro de Usuário
  - Registro
  - Troca de Senha
- Sistema de Login com Token
  - "Esqueci a Senha"
- Sistema de Leitura de Mensagens
  - Endpoint para Start
    - Recebe usuários de tiktok, youtube e twitch para ler mensagens/comentários
    - Apaga todas as mensagens antigas para aquele usuário (delete)
    - Começa a ler as transmissões
  - Endpoint para Stop
    - Encerra o processamento de leitura
  - Salvar mensagens na base
  - Identificar se tem algum comando no começo da mensagem
  - Se tiver comando, processar adequadamente
- Sistema de Comandos
  - !fila -> insere o autor na fila
  - !pontos -> mostrar mensagem de quantos pontos aquele autor
- Sistema de Fila
  - Endpoint para remover da fila (Lógica)

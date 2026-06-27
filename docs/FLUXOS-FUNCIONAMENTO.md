# Fluxos de Funcionamento do Sistema

Documento de consolidação dos fluxos atualmente implementados no backend Live Chat Bridge, com base no estado real do código e dos testes.

## Escopo da análise

- API HTTP (autenticação e ingestão)
- Pipeline de autorização
- Processamento assíncrono do worker
- Regras transversais de idempotência, fila, comando e persistência

## Visão geral dos fluxos ativos

Atualmente o sistema opera com 4 fluxos principais:

1. Registro de conta
2. Login e emissão de JWT
3. Ingestão HTTP autenticada de mensagem
4. Processamento assíncrono de mensagens de live (TikTok -> channel -> ingest)

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
6. Retorna token no envelope padrão Result&lt;T&gt;.

Saídas esperadas:

- 200 OK: token emitido
- 401 Unauthorized: credenciais inválidas
- 500 Internal Server Error: falha ao gerar token

Arquivos base:

- src/LCB.Api/Endpoints/AuthEndpoints.cs
- src/LCB.Application/Commands/Login/LoginHandler.cs

## Fluxo 3 - Ingestão HTTP autenticada de mensagem

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
6. Se mensagem indicar entrada em fila, atualiza/inclui registro em Queue.
7. Adapter parseia texto e tenta despachar comando registrado.
8. Mensagem é marcada como processada e persistida.

Saídas esperadas:

- 200 OK: mensagem processada (ou erro interno encapsulado no status da resposta)
- 400 Bad Request: duplicata já processada (Status = Duplicate)
- 401 Unauthorized: token ausente/inválido
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

## Fluxo 4 - Processamento assíncrono do worker de live

Objetivo:

Consumir mensagens de live (TikTok) e reaproveitar exatamente o caso de uso de ingestão para manter regras unificadas.

Entrada:

- Origem: TikTokLive_Sharp via TikTokChatProvider
- Transporte interno: ChannelReader/ChannelWriter de ChatMessageModel

Processamento:

1. ChatWorker inicia o processamento do canal e a rotina de conexão com o TikTok.
2. Provider escreve mensagens no channel em memória.
3. ChatProcessorService lê mensagens continuamente.
4. Mapper converte WorkerInput em ChatMessageEntity.
5. Service valida author, text e timestamp.
6. Service executa MessageIngestHandler em escopo DI por mensagem.
7. Em falhas não duplicadas, aplica retry até 3 tentativas com backoff.
8. Em status Processed ou Duplicate, encerra ciclo da mensagem.

Saídas esperadas:

- Processamento real com os mesmos efeitos do fluxo HTTP (idempotência, fila, comando, persistência)
- Logs estruturados por mensagem com status e erro
- Reconexão automática do provider em caso de falha

Arquivos base:

- src/LCB.Application/Workers/ChatWorker.cs
- src/LCB.Application/Services/ChatProcessorService.cs
- src/LCB.Application/Services/WorkerInputMapper.cs

## Regras transversais de funcionamento

### Contrato de resposta

- Endpoints retornam envelope ResultT em sucesso e erro.
- Conversão para status HTTP ocorre em ResultExtensions.

### Autorização

- Rotas públicas: /auth/login e /auth/register.
- Rotas protegidas: /messages/ingest e fallback para demais rotas não anônimas.

### Persistência

- Banco atual: SQLite com EF Core e migrations.
- Entidades principais persistidas: Users, Queues, ChatMessages.

### Observabilidade

- CorrelationId middleware e logging padronizado.
- OperationExecutor centraliza início/fim/falha de operações.

## Fluxos planejados (ainda não ativos)

Os seguintes fluxos existem como planejamento e não estão implementados no comportamento atual:

- Acionamento do worker pelo front (spec 18)
- Tabela de logs com auditoria mínima (spec 15)
- Auditoria de origem de inserção em ChatMessages (spec 16)
- Mitigação de durabilidade com replay (spec 17)

Referências:

- docs/specs/planned/18-acionamento-do-worker-pelo-front.md
- docs/specs/planned/15-tabela-logs-com-auditoria-minima.md
- docs/specs/planned/16-campo-auditoria-origem-insercao-chatmessages.md
- docs/specs/planned/17-mitigacao-durabilidade-worker-replay-e-auditoria.md

## Evidências de validação

- Testes de integração cobrem auth e ingestão (incluindo token ausente/inválido/válido e duplicata).
- Execução de referência registrada no projeto: dotnet test LCB.sln com 98 testes aprovados e 0 falhas.

## Sistemas

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

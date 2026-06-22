# Live Chat Bridge Backend - Spec Driven Guide para IA

> Status: rascunho vivo. Este arquivo deve ser atualizado sempre que uma decisão de produto, arquitetura, design ou processo mudar.

Este spec orienta futuras interações com ferramentas de IA como Codex, GitHub Copilot, ChatGPT ou agentes similares. Use-o como fonte primária antes de propor código, refatorações, testes, automações ou mudanças de produto.

## 1. Contexto do Produto

O projeto implementa um backend em .NET para centralizar ingestão e processamento de mensagens de chat de lives.

Hoje existem dois eixos principais no produto:

- API HTTP para autenticação e ingestão manual/programática de mensagens;
- worker em background para captar eventos de live do TikTok e encaminhá-los para processamento interno.

O objetivo é servir como ponte entre provedores de live chat e uma camada de automação, fila, comando e distribuição para frontend.

O sistema ainda está em fase inicial/prototipal: já possui persistência local durável via SQLite/EF Core, há comandos de chat simples, e parte do fluxo assíncrono está preparada mas ainda com lógica pendente.

## 2. Funcionalidades Existentes

- Login por e-mail via `POST /auth/login`, com emissão de JWT quando o usuário é encontrado em repositório persistente.
- Ingestão de mensagens via `POST /messages/ingest`, convertendo payload HTTP em `LCB.Domain.Entities.ChatMessageEntity`.
- Detecção de comandos no texto da mensagem por `AdapterService`, com dispatch para handlers registrados em `CommandRegistry`.
- Comando `!fila`, que hoje retorna resposta de sucesso simulada pelo `FilaCommandHandler`.
- Comando `!comando`, que hoje retorna resposta de sucesso simulada pelo `TestCommandHandler`.
- Atualização de fila persistida para usuários que enviam mensagens reconhecidas por `ShouldJoinQueue()`.
- **Persistência durável** para `UserEntity`, `QueueEntity` e `ChatMessageEntity` via EF Core com SQLite local (Spec 03 ✅).
- **Migrations versionadas** para evolução controlada do schema; índices otimizados e preparação para PostgreSQL.
- Worker hospedado (`ChatWorker`) com tentativa contínua de conexão no TikTok usando `TikTokLive_Sharp`.
- Canal em memória (`System.Threading.Channels`) para receber mensagens do provedor e entregá-las ao `ChatProcessorService`.
- Conversor JSON tolerante para `DateTime` nas entradas HTTP.
- **Logging padronizado** com `TemplateLoggerProvider`, middleware de correlação por request e `OperationExecutor` (Spec 13 ✅).
- Tratamento centralizado de erros com logs estruturados e sem exposição de dados sensíveis.

## 3. Funcionalidades Planejadas

As mini-specs ficam em `docs/specs/` e são organizadas por status em `planned/`, `active/` e `done/`. A estrutura já existe no repositório. Consulte a mini-spec correspondente antes da implementação.

Antes de implementar qualquer item planejado, a IA deve pedir ou propor uma mini-spec no formato da seção 16 deste documento.

**Fallback enquanto não houver mini-spec formal:** se a pasta `docs/specs/planned/` não contiver um arquivo para a funcionalidade solicitada, a IA deve propor um rascunho de mini-spec ao usuário antes de escrever qualquer código, e aguardar confirmação.

### Status Atual de Planejamento

- **Ativas:** Nenhuma (pasta `active/` vazia)
- **Planejadas:** 13 specs em `docs/specs/planned/`
- **Concluídas:** 2 specs em `docs/specs/done/` (03 - Persistência; 13 - Observabilidade)

### Próximas Prioridades Sugeridas

1. **Spec 01** - Corrigir idempotência de mensagens (aceita chaves únicas corretas)
2. **Spec 02** - Autenticação com validação de senha
3. **Spec 04** - Consolidação modelo de mensagem entre fluxo HTTP e worker
4. **Spec 05** - Processamento real em `ChatProcessorService`

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
- Testes unitários com xUnit.
- Persistência atual via EF Core com SQLite local e migrations versionadas.

## 5. Estrutura de Pastas

- `.github`: workflows, Dependabot, assets e instruções para Copilot.
- `docs`: documentação principal do projeto. Contém este spec e a árvore `docs/specs/` com as pastas `planned/`, `active/` e `done/` para mini-specs.
- `src/LCB.Api`: entrypoint HTTP, DI, endpoints, middleware, logging e extensões de API.
- `src/LCB.Application`: handlers de caso de uso, configuração, serviços de processamento e workers.
- `src/LCB.Domain`: contratos, entidades, enums, DTOs, objetos de resultado e modelos compartilhados.
- `src/LCB.Infrastructure`: repositórios persistentes EF Core, handlers de comando, provedores externos, serviços concretos e migrations.
- `test/LCB.UnitTest`: testes unitários de handlers, serviços, workers e repositórios persistentes.

## 6. Fluxos Principais

### Login

> **Estado atual: provisório.** Este fluxo deve ser substituído por uma implementação com validação real de senha. Uma mini-spec deverá criada antes de qualquer mudança nessa área.

1. `AuthEndpoints` recebe `POST /auth/login`.
2. `LoginHandler` busca usuário por e-mail em `IUserRepository`.
3. Se encontrado (independentemente da senha fornecida), `JwtTokenService` gera token com `NameIdentifier` e `Email`.
4. A resposta é convertida por `ResultExtensions.ToMinimalResult()`.

**Comportamento esperado (não implementado):** a autenticação deve comparar o hash da senha fornecida com o `PasswordHash` armazenado na entidade `User` antes de emitir o token.

### Ingestão HTTP de Mensagens

1. `MessageEndpoints` recebe `POST /messages/ingest`.
2. `MessageIngestHandler` converte o request para `LCB.Domain.Entities.ChatMessageEntity`.
3. O handler tenta verificar duplicidade por `IdempotencyKey`.
4. Se a mensagem indicar entrada em fila, o usuário é inserido/atualizado em `IQueueRepository`.
5. O texto é parseado e despachado pelo `AdapterService`.
6. A mensagem é marcada como processada e salva em `IMessageRepository`.

### Worker de Live

1. `ChatWorker` inicia `ChatProcessorService` e conexão com `TikTokChatProvider`.
2. O provedor escreve mensagens em um `ChannelWriter<LCB.Domain.Models.ChatMessageEntity>`.
3. `ChatProcessorService` consome o `ChannelReader` e hoje apenas escreve no console.

## 7. Configuração e Ambiente

- `appsettings.json` define `JWT_KEY` e a seção `Usernames` com `Tiktok`, `Twitch` e `Youtube`.
- `appsettings.Development.json` já contém uma chave JWT de desenvolvimento e um username de TikTok preenchido.
- `ConnectionStrings:DefaultConnection` define o banco SQLite local.
- `LiveConfig.SectionName` aponta para `Usernames`.
- O Swagger só é exposto em ambiente de desenvolvimento.
- A autenticação JWT depende de `JWT_KEY` com pelo menos 32 bytes; caso contrário, o helper retorna `null`.

## 8. Diretrizes de Código

Ao trabalhar neste projeto, a IA deve:

- preservar a separação atual entre `Api`, `Application`, `Domain` e `Infrastructure`;
- manter contratos no `Domain` e implementações concretas fora dele;
- evitar introduzir dependência direta de infraestrutura dentro de endpoints;
- manter handlers pequenos, com regras de orquestração e retorno por `Result<T>`;
- preferir mudanças incrementais, porque há partes ainda prototipais e não totalmente consolidadas;
- adicionar ou atualizar testes ao mexer em repositórios, parsing de comandos, autenticação ou idempotência;
- documentar no spec qualquer mudança que altere fluxo de ingestão, autenticação, processamento ou persistência.

## 9. Convenção de Namespaces de Domínio

O projeto divide os tipos de domínio em três categorias com papéis fixos. A IA deve respeitar essa separação ao propor qualquer novo tipo ou modificar tipos existentes, independente do domínio de negócio envolvido.

| Namespace | Papel | Regra |
| --- | --- | --- |
| `LCB.Domain.Entities` | **Modelo de persistência.** Representa a entidade real de negócio; é o único tipo que trafega entre Application e Infrastructure e será mapeado para o banco de dados. | Nunca adaptar para conveniência de API ou de camada interna. Alterações em Entities afetam persistência e devem passar por mini-spec. |
| `LCB.Domain.DTO` | **Transporte interno entre camadas.** Usado por handlers, serviços e workers para trocar dados sem expor a entidade completa. Pode ser ajustado livremente conforme a necessidade de cada caso de uso. | Nunca retornar diretamente em endpoints de API. Não persiste. |
| `LCB.Domain.Models` | **Response de API.** Representa o contrato de saída dos endpoints — é o que o cliente externo recebe. | Nunca usar como modelo de persistência. A conversão de `Entity → Model` deve acontecer nos handlers (`Application`), nunca em `Infrastructure` ou nos endpoints diretamente. |

**Observação sobre `LCB.Domain.Models.ChatMessageEntity`:** este tipo, usado no canal assíncrono do worker (`TikTokChatProvider → ChannelWriter → ChatProcessorService`), funciona hoje como transporte interno (equivalente funcional a DTO nesse contexto). A conversão para `LCB.Domain.Entities.ChatMessageEntity` deve ocorrer em `ChatProcessorService` antes de qualquer persistência ou lógica de negócio.

### Contratos atuais

- `LCB.Domain.Entities.ChatMessageEntity`: entidade de mensagem; usada em persistência e lógica de negócio no fluxo HTTP.
- `LCB.Domain.Entities.QueueEntity`: entrada de usuário na fila; identidade por `Id` (Guid), indexado por `User`.
- `LCB.Domain.Entities.UserEntity`: usuário autenticável; `Email` como identificador único, `PasswordHash` para validação futura de senha.
- `LCB.Domain.Objects.Result<T>`: envelope padrão para retorno de handlers; sempre use `Result<T>.Ok()` ou `Result<T>.Fail()` — nunca lance exceção para erros de negócio esperados.
- `LCB.Domain.DTO.CommandDTO` / `ParsedCommandDTO`: transporte de resultado de comando e de comando parseado, respectivamente.

## 10. Limitações e Riscos Conhecidos

### Status de Specs Completadas

- ✅ **Spec 03 - Persistência durável** (feita): Repositórios EF Core com SQLite, migrations versionadas, índices otimizados.
- ✅ **Spec 13 - Observabilidade e tratamento de erros** (feita): Logging centralizado, `OperationExecutor`, remoção de `Console.WriteLine` em componentes críticos.

### Idempotência (defeito ativo)

A implementação atual está incorreta: `IdempotencyKey` é definido como `{Provider}:{Timestamp}:{Id}`, onde `Id` é sempre um `Guid.NewGuid()` novo. Isso torna todas as mensagens únicas independentemente do conteúdo, o que quebra a garantia de não duplicar mensagens.

**Comportamento esperado (a implementar via Spec 01 em planejamento):**

- A chave de idempotência deve ser derivada de `Provider + Author + Timestamp`, sem incluir `Id`.
- Se uma mensagem com a mesma chave já tiver sido processada (`Processed == true`), a API deve retornar `400 Bad Request` com `StatusResultEnum.Duplicate` — comportamento já parcialmente implementado no `MessageIngestHandler`, aguardando apenas a correção da chave.
- Se a mensagem existir mas ainda não processada, deve ser tratada como nova tentativa e reprocessada normalmente.

### Autenticação (provisório)

- Login não valida senha. Detalhes na seção 6. Spec 02 planejada para implementar validação.

### Persistência

- Persistência local agora usa SQLite em arquivo `.db` com schema gerenciado por migrations.
- Estratégia de banco online (PostgreSQL) permanece planejada para Spec 14.
- Ainda não há mecanismo de backup, replay ou snapshot.

### Processamento

- `ChatProcessorService` não implementa lógica de negócio real; apenas imprime no console.
- Consolidação de modelo entre fluxo HTTP e worker planejada para Spec 04.
- Lógica real de processamento prevista para Spec 05.

## 11. Testes e Cobertura Atual

- Há cobertura unitária para handlers de login/ingestão, serviços de autenticação, workers e repositórios persistentes.
- Repositórios EF (`UserRepository`, `QueueRepository`, `ChatMessageRepository`) possuem testes com SQLite em memória.
- Há cobertura de `RepositoryBase` para fluxos de sucesso e erro.

## 12. Convenções Observadas

- Injeção de dependência concentrada em métodos de extensão `Add...`.
- Endpoints minimalistas delegando a handlers de aplicação.
- Uso de `Result<T>` para padronizar retornos de sucesso/erro.
- Logging com mensagens de início/fim de método em vários componentes.
- Repositórios usam `RepositoryBase` para padronizar logging e tratamento de exceções.

## 13. Pendências de Documentação

- A estrutura `docs/specs/planned/`, `docs/specs/active/` e `docs/specs/done/` já existe no repositório.
- Manter mini-spec `01-idempotencia-de-mensagens.md` atualizada antes de corrigir a chave de idempotência.
- Manter mini-spec `02-autenticacao-com-senha.md` atualizada antes de implementar validação de senha.
- Atualizar README quando o fluxo de autenticação ou de ingestão mudar de forma material.

## 14. Prioridades Técnicas Evidentes

A ordem de implementação é sempre definida pelo usuário. A IA pode sugerir uma sequência, mas nunca deve impô-la ou assumir que a ordem abaixo é mandatória.

- corrigir idempotência antes de expandir integrações de entrada;
- definir persistência real antes de depender de fila/mensagens entre reinícios;
- ampliar cobertura de testes nas rotas e handlers centrais.

## 15. Como a IA Deve Trabalhar Neste Projeto

### Antes de qualquer ação (codificação, criação de mini-spec, refatoração ou documentação)

1. **reler `docs/SPEC.md`** para verificar se houve alterações feitas pelo usuário desde a última leitura;
2. verificar os arquivos diretamente envolvidos;
3. confirmar se a solicitação muda produto, persistência ou dependências;
4. propor uma mini-spec quando o pedido estiver ambíguo ou afetar comportamento de usuário.

### Antes de criar uma mini-spec, adicionalmente

1. listar todos os arquivos existentes em `docs/specs/planned/`, `docs/specs/active/` e `docs/specs/done/`;
2. ler o conteúdo de cada mini-spec existente e verificar se a nova proposta interfere em alguma delas (sobreposição de escopo, dependência implícita, conflito de comportamento ou alteração de contrato compartilhado);
3. se houver qualquer interferência, descrever o conflito ao usuário e aguardar instrução explícita antes de criar o arquivo;
4. nunca determinar a ordem de implementação de mini-specs — a numeração indica apenas sequência de criação. A IA pode sugerir uma ordem ao usuário, mas a decisão final é sempre do usuário.

### Durante a implementação

- seguir os padrões existentes de pasta, nomes e componentes;
- manter alterações pequenas e rastreáveis;
- não refatorar código não relacionado;
- proteger dados persistidos;
- preservar comportamento existente por padrão;
- pedir confirmação quando uma mudança alterar comportamento de usuário, formato de dados, formato de compartilhamento ou compatibilidade;
- atualizar este spec quando uma decisão nova for tomada;
- atualizar README somente quando a informação for útil para usuários ou contribuidores.

### Ao finalizar

- descrever arquivos alterados;
- informar verificações executadas;
- declarar riscos ou pendências;
- apontar decisões que ainda precisam de confirmação humana.

## 16. Template de Mini-Spec para Novas Features

As mini-specs do projeto ficam em `docs/specs/` e são separadas por status. Novas mini-specs devem nascer em `docs/specs/planned/`, migrar para `docs/specs/active/` quando entrarem em execução e ir para `docs/specs/done/` quando virarem referência estável. Elas devem ser escritas em pt-BR, incluindo acentuação e caracteres especiais.

**Protocolo obrigatório antes de criar uma mini-spec** (ver também segundo bloco da seção 15):

1. Reler `docs/SPEC.md` para garantir que reflete o estado atual do projeto.
2. Listar e ler todas as mini-specs existentes em `planned/`, `active/` e `done/`.
3. Identificar se a nova mini-spec interfere em qualquer mini-spec existente.
4. Se houver interferência de qualquer tipo, apresentar o conflito ao usuário e aguardar definição antes de criar o arquivo.

Convenção obrigatória de mini-spec:

- nome de arquivo com prefixo numérico sequencial **de criação** (não de implementação): `NN-nome-da-feature.md`;
- a numeração indica apenas a ordem em que a mini-spec foi criada — **a ordem de implementação é sempre definida pelo usuário**;
- campo `Número: NN` logo após o título;
- campo `Status:` mantido e atualizado conforme o estágio (`planejado`, `ativa`, `implementado`, `concluída`).

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

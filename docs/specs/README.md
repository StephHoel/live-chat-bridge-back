# Mini-specs do Projeto

Esta pasta organiza mini-specs por status para funcionalidades e pendências do projeto.

Use estes documentos antes de iniciar qualquer alteração de produto. Quando uma mini-spec mudar de estágio, mova o arquivo para a pasta correspondente, atualize o status no documento, registre decisões tomadas durante a execução e mantenha o `docs/SPEC.md` sincronizado quando houver mudança de diretriz.

Diretriz transversal vigente para specs planejadas e futuras:

- O sistema deve suportar N usuários simultâneos conectados.
- A arquitetura deve permitir múltiplos workers concorrentes, com isolamento por usuário/sessão (um worker lógico por usuário ativo).
- Evitar premissas de worker único global em novos contratos e critérios de aceite.

## Fluxo obrigatório de ciclo de vida

1. Ao iniciar a implementação de uma mini-spec:
   - atualizar o status da mini-spec para **em andamento**;
   - mover o arquivo de `planned/` para `active/`;
   - atualizar este `README.md` para refletir a mini-spec na lista **Ativas/Em andamento**.

2. Ao abrir PR com a implementação concluída da mini-spec:
   - atualizar o status da mini-spec para **implementado**;
   - mover o arquivo de `active/` para `done/`;
   - atualizar este `README.md` para refletir a mini-spec no final da lista **Implementadas**.

## Estrutura

- `planned/`: mini-specs planejadas, ainda não iniciadas.
- `active/`: mini-specs em andamento ou em evolução contínua.
- `done/`: mini-specs concluídas, implementadas ou mantidas como referência de decisões já incorporadas.
- `discontinued/`: mini-specs descontinuadas, mantidas apenas como histórico de decisão.

## Status das mini-specs

### ✅ Implementadas (ordem de implementação)

- [Idempotência de mensagens](done/01-idempotencia-de-mensagens.md)
- [Autenticação com validação de senha](done/02-autenticacao-com-senha.md)
- [Persistência durável para repositórios](done/03-persistencia-duravel-repositorios.md)
- [Registro de conta](done/12-registro-de-conta.md)
- [Refatoração transversal de observabilidade e tratamento de erros](done/13-refatoracao-observabilidade-e-tratamento-erros.md)
- [Consolidação do modelo de mensagem entre HTTP e worker](done/04-consolidacao-modelo-de-mensagem.md)
- [Processamento real de mensagens no ChatProcessorService](done/05-processamento-real-chat-worker.md)
- [Segurança de acesso por token para HTTP e acionamento do worker](done/11-seguranca-basica-ingest-token-header.md)
- [Configuração persistida de live e usernames por plataforma](active/19-configuracao-persistida-de-live-e-usernames.md)

### 🔄 Ativas/Em andamento

- _Nenhuma ativa._

### 📋 Planejadas (ordem de prioridade)

**Recomendação técnica:** priorizar segurança transversal e auditoria antes de novas expansões de domínio.

- [Acionamento do worker pelo front](planned/18-acionamento-do-worker-pelo-front.md)
- [Campo de auditoria de origem de inserção em ChatMessages](planned/16-campo-auditoria-origem-insercao-chatmessages.md)
- [Tabela de logs com auditoria mínima](planned/15-tabela-logs-com-auditoria-minima.md)
- [Endpoint de recuperação de acesso](planned/06-endpoint-auth-recover.md)
- [Endpoint HTTP para bootstrap da fila](planned/07-endpoint-http-fila-bootstrap.md)
- [Domínio de pontos, repositório e regras por plataforma](planned/08-dominio-pontos-repositorio-e-regras.md)
- [Use case de pontuação e evento points_updated](planned/09-award-points-e-evento-points-updated.md)
- [Comandos iniciais e registro no dispatcher](planned/10-comandos-iniciais-e-dispatcher.md)
- [Mitigação de durabilidade do worker com replay e auditoria](planned/17-mitigacao-durabilidade-worker-replay-e-auditoria.md)
- [Migração de provider de banco para PostgreSQL](planned/14-migracao-provider-postgresql.md)
- [Nome de usuário para auditoria operacional](planned/21-nome-de-usuario-para-auditoria-operacional.md)

**Nota:** A ordem acima reflete sugestão técnica. O usuário sempre define a priorização final.

### ⛔ Descontinuadas

- [Login retorna usernames de live para bootstrap do front](discontinued/20-login-retorna-usernames-de-live.md)

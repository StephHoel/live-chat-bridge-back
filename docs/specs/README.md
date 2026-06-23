# Mini-specs do Projeto

Esta pasta organiza mini-specs por status para funcionalidades e pendências do projeto.

Use estes documentos antes de iniciar qualquer alteração de produto. Quando uma mini-spec mudar de estágio, mova o arquivo para a pasta correspondente, atualize o status no documento, registre decisões tomadas durante a execução e mantenha o `docs/SPEC.md` sincronizado quando houver mudança de diretriz.

## Estrutura

- `planned/`: mini-specs planejadas, ainda não iniciadas.
- `active/`: mini-specs em andamento ou em evolução contínua.
- `done/`: mini-specs concluídas, implementadas ou mantidas como referência de decisões já incorporadas.

## Status das mini-specs

### ✅ Implementadas (ordem de implementação)

- [Refatoração transversal de observabilidade e tratamento de erros](done/13-refatoracao-observabilidade-e-tratamento-erros.md)
- [Persistência durável para repositórios](done/03-persistencia-duravel-repositorios.md)
- [Idempotência de mensagens](done/01-idempotencia-de-mensagens.md)

### 🔄 Ativas/Em andamento

_Nenhuma no momento._

### 📋 Planejadas (ordem de prioridade)

**Recomendação técnica:** implementar nessa ordem para solidificar a base (idempotência → autenticação → modelo unificado → processamento real).

- [Registro de conta](planned/12-registro-de-conta.md)
- [Autenticação com validação de senha](planned/02-autenticacao-com-senha.md)
- [Consolidação do modelo de mensagem entre HTTP e worker](planned/04-consolidacao-modelo-de-mensagem.md)
- [Processamento real de mensagens no ChatProcessorService](planned/05-processamento-real-chat-worker.md)
- [Endpoint de recuperação de acesso](planned/06-endpoint-auth-recover.md)
- [Endpoint HTTP para bootstrap da fila](planned/07-endpoint-http-fila-bootstrap.md)
- [Domínio de pontos, repositório e regras por plataforma](planned/08-dominio-pontos-repositorio-e-regras.md)
- [Use case de pontuação e evento points_updated](planned/09-award-points-e-evento-points-updated.md)
- [Comandos iniciais e registro no dispatcher](planned/10-comandos-iniciais-e-dispatcher.md)
- [Segurança básica do ingest por token em header](planned/11-seguranca-basica-ingest-token-header.md)
- [Migração de provider de banco para PostgreSQL](planned/14-migracao-provider-postgresql.md)

**Nota:** A ordem acima reflete sugestão técnica. O usuário sempre define a priorização final.

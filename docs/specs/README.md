# Mini-specs do Projeto

Esta pasta organiza mini-specs por status para funcionalidades e pendências do projeto.

Use estes documentos antes de iniciar qualquer alteração de produto. Quando uma mini-spec mudar de estágio, mova o arquivo para a pasta correspondente, atualize o status no documento, registre decisões tomadas durante a execução e mantenha o `docs/SPEC.md` sincronizado quando houver mudança de diretriz.

## Estrutura

- `planned/`: mini-specs planejadas, ainda não iniciadas.
- `active/`: mini-specs em andamento ou em evolução contínua.
- `done/`: mini-specs concluídas, implementadas ou mantidas como referência de decisões já incorporadas.

## Status das mini-specs

### Implementadas

### Ativas/Em andamento

- [13 - Refatoração transversal de observabilidade e tratamento de erros](active/13-refatoracao-observabilidade-e-tratamento-erros.md)

### Planejadas (ordem de prioridade)

- [03 - Persistência durável para repositórios](planned/03-persistencia-duravel-repositorios.md)
- [12 - Registro de conta](planned/12-registro-de-conta.md)
- [02 - Autenticação com validação de senha](planned/02-autenticacao-com-senha.md)
- [01 - Idempotência de mensagens](planned/01-idempotencia-de-mensagens.md)
- [04 - Consolidação do modelo de mensagem entre HTTP e worker](planned/04-consolidacao-modelo-de-mensagem.md)
- [05 - Processamento real de mensagens no ChatProcessorService](planned/05-processamento-real-chat-worker.md)
- [06 - Endpoint de recuperação de acesso](planned/06-endpoint-auth-recover.md)
- [07 - Endpoint HTTP para bootstrap da fila](planned/07-endpoint-http-fila-bootstrap.md)
- [08 - Domínio de pontos, repositório e regras por plataforma](planned/08-dominio-pontos-repositorio-e-regras.md)
- [09 - Use case de pontuação e evento points_updated](planned/09-award-points-e-evento-points-updated.md)
- [10 - Comandos iniciais e registro no dispatcher](planned/10-comandos-iniciais-e-dispatcher.md)
- [11 - Segurança básica do ingest por token em header](planned/11-seguranca-basica-ingest-token-header.md)

# Mini-spec: Endurecimento de concorrência do worker e robustez de configuração de segurança

Número: 28
Status: planejado
Prioridade: alta
Origem: hardening técnico do controle operacional por usuário e da configuração de segurança da API

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- O ciclo operacional do worker por usuário (`start/stop/status`) já existe, mas ainda pode sofrer corrida entre comandos concorrentes para o mesmo usuário.
- Sem serialização por usuário e transições atômicas de estado, chamadas concorrentes podem causar estado inconsistente (double-start, stop sobre estado antigo, status divergente).
- A configuração de segurança já possui baseline de autenticação, porém faltam guard rails transversais para falhas de configuração e para endurecimento dos cenários de operação concorrente.

## Objetivo

- Endurecer o controle concorrente do worker por usuário, garantindo transições de estado atômicas e previsíveis.
- Padronizar idempotência e semântica de conflito para comandos operacionais (`start/stop`).
- Consolidar fail-fast de configuração crítica de segurança no startup e em pontos de uso sensíveis.
- Reduzir regressões de segurança e robustez com cobertura de testes de corrida e validações de configuração.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/done/18-acionamento-do-worker-pelo-front.md](../done/18-acionamento-do-worker-pelo-front.md): mantém o contrato dos endpoints, mas fortalece semântica concorrente e transição de estado por usuário.
- Interfere com [docs/specs/done/19-configuracao-persistida-de-live-e-usernames.md](../done/19-configuracao-persistida-de-live-e-usernames.md): reforça validações e consistência operacional das configurações usadas no start do worker.
- Interfere com [docs/specs/done/11-seguranca-basica-ingest-token-header.md](../done/11-seguranca-basica-ingest-token-header.md): preserva política de autenticação centralizada e adiciona hardening de robustez em cenários de configuração/concorrência.
- Interfere com [docs/specs/done/22-swagger-desprotegido-e-testes-integracao.md](../done/22-swagger-desprotegido-e-testes-integracao.md): mantém exceção de Swagger em `Development` sem afrouxar validações de segurança no restante da API.
- Interfere com [docs/specs/planned/27-validacao-explicita-da-chave-jwt.md](27-validacao-explicita-da-chave-jwt.md): esta spec reutiliza as regras de validação explícita de `JWT_KEY` como pré-condição do hardening de configuração.
- Interfere com [docs/specs/planned/17-mitigacao-durabilidade-worker-replay-e-auditoria.md](17-mitigacao-durabilidade-worker-replay-e-auditoria.md): não substitui replay/durabilidade, mas prepara invariantes de concorrência no controle operacional para integração segura futura.

## Comportamento esperado

- `POST /worker/start` e `POST /worker/stop` devem operar com exclusão mútua por usuário autenticado.
- Comandos concorrentes para o mesmo usuário devem seguir uma política determinística:
  - `start` repetido sobre sessão já ativa: resposta idempotente (ou conflito explícito, conforme decisão final de contrato).
  - `stop` repetido sobre sessão já inativa: resposta idempotente (ou conflito explícito, conforme decisão final de contrato).
  - `start` e `stop` simultâneos para o mesmo usuário: apenas uma transição vence de forma atômica; a outra recebe estado consistente.
- Comandos de usuários diferentes não devem competir entre si e devem preservar isolamento de sessão.
- O status retornado por `GET /worker/status` deve refletir estado efetivo e monotônico da sessão do usuário.
- Falhas de configuração crítica de segurança devem ser detectadas cedo, com erro explícito e diagnóstico operacional objetivo.

## Superfícies afetadas

- Endpoints: `POST /worker/start`, `POST /worker/stop`, `GET /worker/status`.
- Serviços de aplicação: controle de estado do worker por usuário e coordenação de listeners.
- Configuração/DI: validações fail-fast de configuração crítica de segurança.
- Observabilidade/auditoria: registro consistente de disputa de concorrência e falhas de pré-condição.

## Dados e persistência

- Nesta fase, não exige novo schema obrigatório.
- Se houver estado operacional em memória, ele deve incluir mecanismo de lock/claim por usuário e versão de estado para evitar lost update.
- Caso haja evolução para persistência de estado operacional, a escrita deve manter compare-and-swap lógico (ou equivalente) por usuário.

## Contratos de API

- Mantém os endpoints atuais de worker e envelope `Result<T>`.
- Deve explicitar semântica para concorrência e idempotência em `start/stop`.
- Códigos HTTP esperados (a confirmar na implementação):
  - `200 OK`: operação efetivada ou idempotente bem-sucedida.
  - `401 Unauthorized`: usuário não autenticado.
  - `409 Conflict`: comando incompatível com estado/transição concorrente, quando não adotado retorno idempotente.
  - `503 Service Unavailable`: indisponibilidade operacional real.

## Regras de validação

- Nenhum comando operacional pode atuar fora do contexto do usuário autenticado da requisição.
- A transição de estado do worker deve ser atômica por usuário.
- Não pode existir mais de uma sessão ativa concorrente do mesmo worker lógico para o mesmo usuário.
- A validação de configuração crítica de segurança (incluindo chave JWT) deve ser única, explícita e reutilizável.
- Erros de configuração crítica devem falhar com mensagem diagnóstica sem vazamento de segredo.

## Critérios de aceite

- Concorrência de `start/stop/status` para o mesmo usuário não produz estado inconsistente.
- Comandos concorrentes para usuários distintos permanecem isolados e funcionais.
- Operações repetidas (retries do cliente) seguem política definida de idempotência/conflicto sem comportamento ambíguo.
- Falhas de configuração crítica de segurança são detectadas de forma determinística antes da operação degradada.
- Logs/auditoria permitem rastrear disputas de estado e motivo de bloqueio/rejeição de comando.

## Testes esperados

- Teste de corrida com múltiplos `start` simultâneos para o mesmo usuário.
- Teste de corrida com múltiplos `stop` simultâneos para o mesmo usuário.
- Teste de corrida cruzada `start` vs `stop` para o mesmo usuário.
- Teste de isolamento com operações simultâneas de usuários diferentes.
- Teste de regressão de contrato (`Result<T>`, códigos HTTP e payload) para endpoints de worker.
- Teste de validação fail-fast para configuração crítica de segurança inválida/ausente.
- Teste garantindo manutenção da exceção de Swagger apenas em `Development` e proteção dos endpoints de negócio.

## Fora de escopo

- Introdução de orquestração distribuída entre múltiplas instâncias da aplicação.
- Migração para broker externo de comandos operacionais.
- Redesenho de autenticação (OAuth, refresh token, multi-chave JWT).
- Mudança de regra funcional de replay/durabilidade da inbox do worker (escopo da Spec 17).

## Plano sugerido de implementação (incremental)

1. Definir política oficial de idempotência vs conflito para `start/stop` e documentar contratos de resposta.
2. Implementar serialização de transição de estado por usuário no serviço de controle do worker.
3. Consolidar validação única de configuração crítica de segurança no startup e no serviço de token.
4. Instrumentar logs/auditoria para disputas de concorrência e rejeições por pré-condição.
5. Cobrir com testes de corrida, isolamento por usuário e regressão de segurança/contrato.

## Adendo específico: remoção de bloqueio síncrono (Wait) e adoção assíncrona

### Contexto do achado

- Foi identificado uso de espera síncrona em semáforo no controle do worker, especificamente em `WorkerControlService` (referência atual: `session.Gate.Wait()`).
- Em cenários de alta concorrência, essa abordagem pode bloquear thread de forma desnecessária e aumentar contenção no pool.

### Decisão para esta spec

- Substituir esperas síncronas em pontos críticos do controle de sessão por mecanismo assíncrono (`await`), preservando exclusão mútua por usuário.
- A seção crítica deve permanecer curta e sem operações bloqueantes externas enquanto o lock estiver adquirido.
- A política de transição de estado deve continuar atômica e consistente com a semântica de idempotência/conflito já definida nesta mini-spec.

### Regras adicionais de implementação

- Evitar `Wait()`/`Wait(int)`/`Wait(CancellationToken)` síncronos em rotas de execução operacional do worker.
- Preferir `WaitAsync(...)` com `CancellationToken` quando houver contexto cancelável.
- Garantir `Release()` em bloco `finally` para evitar vazamento de lock em exceções.
- Não manter lock adquirido durante I/O, chamadas de provider, escrita de auditoria ou atrasos artificiais.

### Critérios de aceite adicionais

- Não há uso remanescente de `Wait()` síncrono no fluxo de controle operacional do worker por usuário.
- A transição para lock assíncrono mantém comportamento funcional de `start/stop/status` sem regressão de contrato HTTP.
- Sob concorrência, não há evidência de starvation operacional causada por bloqueio síncrono no controle de sessão.

### Testes adicionais esperados

- Teste de concorrência reproduzindo múltiplas chamadas simultâneas para o mesmo usuário, validando ausência de regressão funcional após migração para lock assíncrono.
- Teste com cancelamento durante espera de lock para confirmar propagação/encerramento correto do fluxo.
- Teste de robustez garantindo liberação do lock em cenários de exceção dentro da seção crítica.

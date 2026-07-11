# Mini-spec: Estabilização da suíte de testes completa

Número: 26
Status: planejado
Origem: falha intermitente identificada na execução completa de `dotnet test LCB.sln` (teste de integração de Queue passando isolado e falhando em suíte completa)
Prioridade: alta

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- A suíte completa não está estável: há ao menos um cenário de integração (`Queue`) que falha no `dotnet test LCB.sln`, apesar de passar em execução isolada.
- Esse padrão é sintoma de acoplamento indevido entre testes (estado compartilhado, ordem de execução, concorrência entre fixtures ou resíduos de banco/processo).
- Flakiness reduz a confiabilidade do gate de qualidade e aumenta retrabalho em CI/CD.

## Objetivo

- Tornar a execução completa da suíte determinística e reprodutível localmente e no pipeline.
- Eliminar dependências ocultas entre testes de integração e entre projetos de teste.
- Definir baseline de estabilidade mensurável para regressões futuras.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/done/03-persistencia-duravel-repositorios.md](../done/03-persistencia-duravel-repositorios.md): exige reforço de isolamento de persistência nos testes para evitar vazamento de estado entre execuções.
- Interfere com [docs/specs/done/07-endpoint-http-fila-bootstrap.md](../done/07-endpoint-http-fila-bootstrap.md): valida de forma determinística o contrato de ordenação e leitura de fila no cenário de integração.
- Interfere com [docs/specs/done/18-acionamento-do-worker-pelo-front.md](../done/18-acionamento-do-worker-pelo-front.md): testes devem evitar impacto cruzado de estado operacional por usuário entre classes e coleções.
- Interfere com [docs/specs/done/19-configuracao-persistida-de-live-e-usernames.md](../done/19-configuracao-persistida-de-live-e-usernames.md): fixtures e seed devem preservar isolamento por usuário para não gerar colisão em configuração persistida.
- Interfere com [docs/specs/done/22-swagger-desprotegido-e-testes-integracao.md](../done/22-swagger-desprotegido-e-testes-integracao.md): amplia a estratégia de testes de integração para incluir estabilidade da execução em lote, além do contrato funcional.
- Interfere com [docs/specs/planned/25-isolamento-da-fila-por-usuario-e-delecao-logica.md](25-isolamento-da-fila-por-usuario-e-delecao-logica.md): a estabilização dos testes deve preparar base para a evolução do domínio de fila sem introduzir nova flakiness.

## Hipóteses técnicas a validar

- Reuso de banco/arquivo físico entre testes e projetos, sem limpeza completa entre casos.
- Paralelismo de testes de integração disputando o mesmo estado de infraestrutura (`DbContext`, arquivos SQLite, listeners/worker).
- Dependência implícita de ordem em assertions de fila sem critério de desempate determinístico quando necessário.
- Dados de autenticação e seed reaproveitados em cenários diferentes sem escopo único por teste.

## Comportamento esperado

- Cada teste de integração deve ser executável de forma independente, sem depender de ordem global.
- A suíte completa deve produzir o mesmo resultado em execuções repetidas no mesmo commit.
- Cenários de Queue devem manter ordenação determinística e sem interferência de dados residuais.
- A infraestrutura de teste deve explicitar estratégia de isolamento (por teste, por classe ou por coleção) e sua justificativa.

## Superfícies afetadas

- Projetos de teste: `test/LCB.IntegrationTest` (principal) e eventualmente `test/LCB.UnitTest` no que tocar configuração global de execução.
- Infraestrutura de testes: `ApiWebApplicationFactory`, helpers de autenticação/seed, coleções/fixtures xUnit.
- Persistência para testes: estratégia de banco em memória/arquivo temporário e limpeza transacional ou recriação controlada.
- Pipeline: comando e parâmetros de execução da suíte completa.

## Dados e persistência (escopo de teste)

- Definir política única para banco de integração:
  - opção A: banco efêmero por teste/classe;
  - opção B: banco compartilhado por coleção com reset transacional obrigatório entre cenários.
- Proibir dependência de estado residual entre testes de queue, auth, config e worker.
- Garantir seed deterministicamente idempotente e identificadores únicos por cenário quando necessário.

## Contratos de API

- Sem alteração de contrato HTTP funcional.
- Sem alteração de envelope `Result<T>`.
- Qualquer ajuste em testes deve validar comportamento já contratado, sem mudar semântica de endpoint.

## Regras de validação

- Teste que passa isolado deve passar também na execução completa da solução.
- Nenhum teste de integração pode depender de side effects de outro teste.
- Falha intermitente deve ser tratada como bug de suíte, mesmo quando eventual retry mascara o problema.
- Alterações de paralelismo devem ser mínimas e justificadas; desabilitar paralelismo global é último recurso.

## Critérios de aceite

- `dotnet test LCB.sln` passa de forma consistente em execuções repetidas no mesmo ambiente.
- Cenário de Queue reportado como flakey deixa de apresentar falha intermitente.
- Existe documentação objetiva da estratégia de isolamento adotada para integração.
- Pipeline passa a executar a suíte sem necessidade de rerun para o mesmo commit.

## Testes esperados

- Teste de repetição da suíte de integração em lote (múltiplas execuções consecutivas).
- Testes de integração de Queue executados:
  - isoladamente;
  - em conjunto com os demais endpoints;
  - sob a mesma configuração usada no pipeline.
- Testes de infraestrutura de fixture para validar limpeza/reset de estado entre cenários.

## Plano sugerido de implementação (incremental)

1. Reproduzir a falha em modo determinístico e registrar condições de execução.
2. Instrumentar logs de fixture/seed/DbContext para identificar fonte de acoplamento entre testes.
3. Implementar isolamento de persistência e contexto operacional por escopo de teste.
4. Ajustar setup de paralelismo apenas onde houver conflito comprovado.
5. Consolidar asserts determinísticas de Queue (incluindo ordenação e pré-condições de seed).
6. Validar com execuções repetidas de `dotnet test LCB.sln` e publicar evidências na PR.

## Riscos

- Mitigação simplista via desligamento amplo de paralelismo pode aumentar tempo de pipeline sem atacar causa raiz.
- Isolamento incompleto pode mascarar flakiness temporariamente e reaparecer em CI.
- Mudanças em infraestrutura de teste podem afetar tempo de execução da suíte.

## Fora de escopo

- Mudança de regra de negócio dos endpoints.
- Redesenho de domínio da fila nesta entrega.
- Troca de stack de testes ou framework.

## Adendo posterior: isolamento de `JWT_KEY` na factory de integração

- Achado: a infraestrutura de testes de integração (`ApiWebApplicationFactory`) seta e limpa `JWT_KEY` via variável de ambiente global de processo durante o ciclo de vida da factory.
- Risco: em execução paralela de testes, múltiplas classes/fixtures podem disputar a mesma chave global, gerando comportamento não determinístico de autenticação/autorização.
- Impacto observado: instabilidade intermitente em cenários de integração sensíveis a JWT (especialmente fluxos de autenticação e endpoints protegidos).

### Diretriz de mitigação deste adendo

- Tratar `JWT_KEY` de teste como configuração isolada por escopo de execução (por factory/classe/coleção), evitando mutação global compartilhada entre testes concorrentes.
- Garantir restauração determinística de qualquer estado global eventualmente alterado pela infraestrutura de teste.
- Priorizar injeção/configuração local da aplicação de teste (ex.: `IConfiguration`/host builder de teste) em vez de `Environment.SetEnvironmentVariable` global.

### Critério adicional de aceite

- A suíte de integração executada em paralelo não deve apresentar flakiness causada por corrida de escrita/leitura em `JWT_KEY` global.

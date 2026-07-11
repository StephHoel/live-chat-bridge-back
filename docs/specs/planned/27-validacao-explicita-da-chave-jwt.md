# Mini-spec: Validação explícita da chave JWT

Número: 27
Status: planejado
Prioridade: média
Origem: achado de robustez na configuração de autenticação JWT da API

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- A configuração de autenticação JWT consome `JWT_KEY` sem tratar explicitamente o cenário de chave ausente, inválida ou curta antes de montar `TokenValidationParameters`.
- O helper atual de conversão de chave pode retornar `null`, o que deixa o comportamento dependente de falha indireta ao construir `SymmetricSecurityKey` ou de retorno vazio na emissão de token.
- O resultado atual é frágil: uma configuração incorreta de ambiente pode quebrar o boot da API ou a autenticação em tempo de execução sem diagnóstico claro e padronizado.

## Objetivo

- Tornar explícita a validação de `JWT_KEY` no startup da API e no serviço de emissão de token.
- Falhar de forma determinística e diagnóstica quando a chave JWT estiver ausente ou inválida.
- Eliminar o fluxo silencioso baseado em `null` ou retorno vazio para erro de configuração crítica de autenticação.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/done/02-autenticacao-com-senha.md](../done/02-autenticacao-com-senha.md): o login continua validando credenciais como hoje, mas a emissão do JWT passa a depender de pré-condição de configuração validada explicitamente.
- Interfere com [docs/specs/done/11-seguranca-basica-ingest-token-header.md](../done/11-seguranca-basica-ingest-token-header.md): reforça a fundação da autenticação centralizada no pipeline ao endurecer a configuração da chave usada para validar tokens.
- Interfere com [docs/specs/done/13-refatoracao-observabilidade-e-tratamento-erros.md](../done/13-refatoracao-observabilidade-e-tratamento-erros.md): erros de configuração devem produzir mensagem diagnóstica clara, log estruturado e comportamento previsível, sem fallback silencioso.
- Interfere com [docs/specs/done/22-swagger-desprotegido-e-testes-integracao.md](../done/22-swagger-desprotegido-e-testes-integracao.md): a exceção de autenticação do Swagger em `Development` não altera a exigência de chave JWT válida para o restante do pipeline protegido.

## Comportamento esperado

- A API deve validar `JWT_KEY` antes de configurar `AddJwtBearer` e antes de disponibilizar o serviço de geração de token.
- Chave ausente, vazia, inválida ou com menos de 32 bytes deve gerar falha explícita de configuração, com mensagem objetiva para operador.
- A validação da chave deve ter um único ponto de regra, reutilizável por configuração do pipeline e por emissão de token.
- O sistema não deve prosseguir em modo degradado para autenticação quando a chave for inválida.
- O fluxo de login não deve retornar token vazio como forma de representar erro de configuração crítica.

## Superfícies afetadas

- API/DI: configuração de autenticação JWT e startup da aplicação.
- Serviços: emissão de token JWT para login/autenticação.
- Logging/observabilidade: mensagens diagnósticas para erro de configuração.
- Testes: cobertura de startup/configuração inválida e emissão de token.

## Dados e persistência

- Sem mudança de schema.
- Sem nova persistência.
- `JWT_KEY` continua vindo de configuração/ambiente, mas sua validação deixa de ser tolerante a erro silencioso.

## Contratos de API

- Não há mudança de contrato HTTP em cenários válidos.
- Em ambiente corretamente configurado, login e endpoints protegidos mantêm comportamento atual.
- Em ambiente inválido, a aplicação deve falhar no boot ou bloquear explicitamente a configuração de autenticação antes de aceitar tráfego, em vez de expor falha tardia por endpoint.

## Regras de validação

- `JWT_KEY` é obrigatória.
- A chave deve ter no mínimo 32 bytes após codificação UTF-8.
- O componente de validação da chave não deve mascarar exceções relevantes retornando `null` genericamente.
- Erro de configuração deve expor mensagem operacional clara e sem vazar outros segredos.
- A mesma regra de validação deve ser usada tanto para `TokenValidationParameters` quanto para geração de token.

## Critérios de aceite

- A aplicação falha explicitamente ao iniciar quando `JWT_KEY` estiver ausente ou inválida.
- A configuração da autenticação não depende mais de `byte[]?` nulo para sinalizar erro.
- O serviço de emissão de token não retorna string vazia para representar falha de configuração de chave.
- Logs e exceções deixam claro que o problema é de configuração de `JWT_KEY` e não de credenciais do usuário.
- Em configuração válida, login e endpoints protegidos continuam funcionando sem regressão de contrato.

## Testes esperados

- Teste unitário da regra de validação da chave com cenários: ausente, vazia, curta e válida.
- Teste unitário/integração do startup garantindo falha explícita quando `JWT_KEY` for inválida.
- Teste unitário do serviço de token garantindo comportamento correto com chave válida e impossibilidade de seguir com chave inválida.
- Teste de regressão de autenticação com configuração válida, preservando emissão e validação de token.

## Fora de escopo

- Rotação de chave JWT.
- Suporte a múltiplas chaves simultâneas.
- Refresh token.
- Alteração de claims ou tempo de expiração do token.
- Mudança de estratégia de autenticação/autorização além do hardening de configuração.

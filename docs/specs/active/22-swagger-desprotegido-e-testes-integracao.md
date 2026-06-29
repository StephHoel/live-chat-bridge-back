# Mini-spec: Swagger desprotegido e testes de integração

Número: 22
Status: em andamento

## Problema

- A documentação do Swagger está exigindo autenticação, o que bloqueia acesso direto à documentação em ambiente de desenvolvimento.
- Não há testes de integração cobrindo disponibilidade dos endpoints de documentação (`/swagger/index.html` e `/swagger/v1/swagger.json`).

## Comportamento esperado

- Expor o Swagger sem autenticação no ambiente de desenvolvimento.
- Manter o restante dos endpoints protegidos conforme política atual de segurança.
- Adicionar testes de integração para validar o acesso público aos endpoints do Swagger.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/done/11-seguranca-basica-ingest-token-header.md](../done/11-seguranca-basica-ingest-token-header.md), que define proteção por token para endpoints HTTP (com exceções explícitas em autenticação).
- Esta mini-spec formaliza exceção adicional para endpoints de documentação do Swagger, limitada ao ambiente de desenvolvimento.

### Decisão explícita do usuário

- Prosseguir com a exceção de segurança para Swagger e incluir cobertura de testes de integração para os endpoints de documentação.
- A exceção de autenticação do Swagger deve valer somente no ambiente `Development`.

## Superfícies afetadas

- Endpoints: `GET /swagger/index.html` e `GET /swagger/v1/swagger.json`.
- Handlers: sem impacto direto.
- Workers/Provedores: sem impacto.
- Integrações externas: sem impacto.

## Dados e persistência

- Sem mudanças de persistência.
- Sem mudanças de schema.

## Contratos de API

- Request:
  - `GET /swagger/index.html` sem header de autenticação.
  - `GET /swagger/v1/swagger.json` sem header de autenticação.
- Response:
  - `200 OK` com conteúdo HTML para `index.html`.
  - `200 OK` com conteúdo JSON OpenAPI para `swagger.json`.
- Códigos HTTP esperados:
  - `200 OK`: endpoints de Swagger acessíveis sem token em ambiente de desenvolvimento.
  - `404 Not Found` ou equivalente: quando Swagger estiver desabilitado por ambiente/configuração.

## Regras de validação

- A exceção de autenticação deve valer apenas para rotas do Swagger no ambiente `Development`.
- Endpoints de negócio devem permanecer protegidos por token.
- O comportamento deve ser consistente com o ambiente de execução (Swagger habilitado apenas onde for permitido).

## Critérios de aceite

- `GET /swagger/index.html` responde com sucesso sem token no ambiente de desenvolvimento.
- `GET /swagger/v1/swagger.json` responde com sucesso sem token no ambiente de desenvolvimento.
- Endpoints protegidos existentes continuam retornando `401 Unauthorized` sem token.
- Testes de integração cobrindo os dois endpoints de Swagger estão implementados e passando.

## Testes esperados

- Teste de integração para `GET /swagger/index.html` sem token, validando `200 OK` e conteúdo HTML.
- Teste de integração para `GET /swagger/v1/swagger.json` sem token, validando `200 OK` e payload OpenAPI.
- Teste de regressão de segurança para endpoint protegido sem token, garantindo manutenção do `401 Unauthorized`.

## Fora de escopo

- Expor Swagger em produção.
- Expor Swagger sem autenticação em qualquer ambiente diferente de `Development`.
- Alterar fluxo de autenticação de endpoints de negócio.
- Alterar contratos de domínio, handlers ou persistência.

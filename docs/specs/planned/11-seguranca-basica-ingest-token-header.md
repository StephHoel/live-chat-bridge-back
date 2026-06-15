# Mini-spec: Segurança básica do ingest por token em header

Número: 11
Status: planejado
Origem: [Issue #45](https://github.com/StephHoel/live-chat-bridge/issues/45)

## Problema

- Endpoint de ingest pode ficar exposto sem proteção mínima.
- Risco de envio não autorizado de comandos/mensagens.

## Comportamento esperado

- Suportar validação opcional de token por header (`x-ingest-token`).
- Se variável de ambiente de token estiver configurada, validar obrigatoriamente.
- Se token não estiver configurado, permitir requisição (modo dev).

## Superfícies afetadas

- Endpoints: `POST /messages/ingest`.
- Handlers: sem alteração de regra de negócio.
- Workers/Provedores: sem impacto.
- Integrações externas: variáveis de ambiente/configuração.

## Dados e persistência

- Não persistir token em logs.
- Registrar apenas tentativa autorizada/não autorizada de forma segura.

## Contratos de API

- Request: header opcional `x-ingest-token`.
- Response: sem alteração no sucesso.
- Códigos HTTP:
  - `200 OK`: autorizado.
  - `401 Unauthorized` ou `403 Forbidden`: token ausente/inválido quando proteção ativa.

## Regras de validação

- Comparação exata com token configurado.
- Ausência de configuração ativa modo permissivo controlado.

## Critérios de aceite

- Com token configurado, requisições sem token são negadas.
- Com token configurado, requisição com token inválido é negada.
- Sem token configurado, fluxo continua funcionando em desenvolvimento.

## Testes esperados

- Teste de autorização com token válido.
- Teste sem token quando proteção ativa.
- Teste sem configuração de token (modo permissivo).

## Fora de escopo

- OAuth, JWT de serviço e mTLS.
- Rate limiting e WAF.

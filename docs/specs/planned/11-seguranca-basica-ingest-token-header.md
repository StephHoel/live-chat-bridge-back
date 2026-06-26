# Mini-spec: Segurança de acesso por token para HTTP e acionamento do worker

Número: 11
Status: planejado
Origem: [Issue #45](https://github.com/StephHoel/live-chat-bridge/issues/45)

## Problema

- Endpoints HTTP sensíveis podem ficar expostos sem proteção mínima uniforme.
- Há necessidade de exigir token para qualquer operação HTTP (exceto autenticação inicial) e para rotas de acionamento operacional do worker.
- Sem padronização, cada endpoint pode adotar uma regra de segurança diferente.

## Comportamento esperado

- Exigir token em todas as requisições HTTP da API, com exceção de `POST /auth/login` e `POST /auth/register`.
- Exigir token em qualquer endpoint que acione/parcialmente controle o worker (quando existente).
- Padronizar o header de autenticação por token de serviço para chamadas não interativas.
- Aplicar autenticação em ponto único do pipeline HTTP (middleware/filtro/policy), sem replicar checagem manual em endpoints e handlers.
- Falhas de autenticação devem retornar erro consistente sem expor segredos.

## Superfícies afetadas

- Endpoints: todos os endpoints HTTP, exceto `POST /auth/login` e `POST /auth/register`.
- Handlers: sem alteração de regra de negócio; proteção deve ocorrer no pipeline (middleware/filtro/autorização).
- Workers/Provedores: impacto indireto em endpoints de acionamento/controle do worker.
- Integrações externas: variáveis de ambiente/configuração de token.

## Dados e persistência

- Não persistir token em logs.
- Registrar apenas tentativa autorizada/não autorizada de forma segura.
- Garantir que logs de segurança não armazenem conteúdo bruto de header de autenticação.

## Contratos de API

- Request: header obrigatório de token para endpoints protegidos.
- Response: sem alteração no sucesso para endpoints autenticados.
- Códigos HTTP:
  - `200 OK`: autorizado.
  - `401 Unauthorized`: token ausente ou inválido em endpoint protegido.
  - `403 Forbidden`: opcional quando token válido não possuir escopo/perfil esperado (se aplicável futuramente).

## Regras de validação

- Comparação exata com token configurado em ambiente.
- `POST /auth/login` e `POST /auth/register` são rotas explicitamente públicas.
- Endpoints protegidos nunca operam em modo permissivo no ambiente alvo da feature.
- Endpoints e handlers não devem conter lógica duplicada de validação de token quando o pipeline central estiver ativo.

## Critérios de aceite

- Requisições para qualquer endpoint protegido sem token retornam `401 Unauthorized`.
- Requisições para qualquer endpoint protegido com token inválido retornam `401 Unauthorized`.
- `POST /auth/login` e `POST /auth/register` permanecem acessíveis sem token.
- Endpoints de acionamento/controle do worker exigem token.
- Não existe duplicação de checagem de token entre middleware/filtro e endpoint/handler.

## Testes esperados

- Testes de autorização para endpoint protegido com token válido.
- Testes para endpoint protegido sem token e com token inválido.
- Testes garantindo acesso público apenas para login e register.
- Testes para endpoint de acionamento/controle do worker validando exigência de token.

## Fora de escopo

- OAuth, JWT de serviço e mTLS.
- Rate limiting e WAF.

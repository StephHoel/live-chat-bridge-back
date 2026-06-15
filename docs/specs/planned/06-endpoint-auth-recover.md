# Mini-spec: Endpoint de recuperação de acesso

Número: 06
Status: planejado
Origem: [Issue #21](https://github.com/StephHoel/live-chat-bridge/issues/21)

## Problema

- Não existe endpoint dedicado para fluxo de recuperação de acesso.
- A ausência desse endpoint impede integração padronizada do frontend.

## Comportamento esperado

- Expor endpoint `POST /api/auth/recover` (equivalente no backend atual) para receber e-mail.
- A rota deve orquestrar requisição e delegar regra de negócio para serviço/use case.
- Resposta deve ser neutra e não revelar existência de conta.

## Superfícies afetadas

- Endpoints: rota de recover.
- Handlers: handler/use case de recuperação.
- Workers/Provedores: sem impacto.
- Integrações externas: provedor de envio de e-mail (quando existir).

## Dados e persistência

- Pode consultar usuário por e-mail sem expor resultado.
- Não persistir dados sensíveis em log.

## Contratos de API

- Request: `{ email: string }`.
- Response: mensagem neutra de confirmação.
- Códigos HTTP:
  - `200 OK`: resposta neutra sempre que request for válido.
  - `400 Bad Request`: payload inválido.

## Regras de validação

- `email` obrigatório e em formato válido.
- Não retornar diferença entre usuário existente e inexistente.

## Critérios de aceite

- Endpoint sem regra de negócio acoplada.
- Serviço centraliza a lógica.
- Resposta neutra implementada conforme issue.

## Testes esperados

- Teste de endpoint para payload válido.
- Teste para payload inválido.
- Teste garantindo resposta neutra para e-mail existente/inexistente.

## Fora de escopo

- Envio real de e-mail.
- Token de reset e tela de redefinição de senha.

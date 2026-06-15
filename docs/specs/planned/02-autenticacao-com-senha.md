# Mini-spec: Autenticação com validação de senha

Número: 02
Status: planejado

## Problema

- O login atual emite JWT apenas com base no e-mail encontrado, sem validar senha.
- Isso cria risco crítico de autenticação indevida.

## Comportamento esperado

- Login deve validar senha fornecida contra `PasswordHash` armazenado.
- JWT só deve ser emitido após validação de credenciais.
- Credenciais inválidas devem retornar resposta de falha sem detalhes sensíveis.

## Superfícies afetadas

- Endpoints: `POST /auth/login`.
- Handlers: `LoginHandler`.
- Workers/Provedores: sem alteração.
- Integrações externas: sem alteração obrigatória.

## Dados e persistência

- Usar `User.PasswordHash` como base para comparação.
- Ajustar `IUserRepository`/implementação para não injetar usuário fake durante leitura.
- Preservar compatibilidade com armazenamento atual em memória até migração de persistência.

## Contratos de API

- Request: manter estrutura atual (`email`, `password`).
- Response: manter retorno de token em sucesso e erro padronizado em falha.
- Códigos HTTP:
  - `200 OK`: credenciais válidas e token emitido.
  - `401 Unauthorized`: e-mail/senha inválidos.

## Regras de validação

- `email` obrigatório e em formato válido.
- `password` obrigatória e não vazia.
- Nunca retornar se o e-mail existe ou não em mensagens de erro.

## Critérios de aceite

- Login com senha incorreta nunca gera token.
- Login com senha correta gera token com claims atuais.
- Fluxo de erro deve ser consistente e sem vazamento de informação.

## Testes esperados

- Teste unitário para sucesso no login com senha válida.
- Teste unitário para falha com senha inválida.
- Teste unitário para usuário inexistente.

## Fora de escopo

- Cadastro de usuário.
- Refresh token.
- MFA e provedores externos de identidade.

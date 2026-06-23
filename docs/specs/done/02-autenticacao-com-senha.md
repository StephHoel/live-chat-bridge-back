# Mini-spec: Autenticação com validação de senha

Número: 02
Status: concluído

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

- Usar `UserEntity.PasswordHash` como base para comparação.
- Ajustar `IUserRepository`/implementação para não injetar usuário fake durante leitura.
- Preservar compatibilidade com persistência atual (EF Core + SQLite) e futura troca de provider para PostgreSQL.

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

---

## Decisões tomadas durante a implementação

- Criado `IPasswordHasher` em `LCB.Domain.Interfaces.Services` como contrato.
- Implementação concreta `PasswordHasher` em `LCB.Infrastructure.Services` usando PBKDF2 com SHA256.
  - **Algoritmo:** PBKDF2 com 10.000 iterações e SHA256
  - **Salt:** 128 bits (16 bytes) gerado aleatoriamente
  - **Hash size:** 256 bits (32 bytes)
  - **Comparação:** Constant-time comparison para prevenir timing attacks
- Resposta de erro unificada: `"Invalid email or password"` em ambos os casos (usuário não encontrado ou senha incorreta) para evitar enumeration attacks.
- Código HTTP mudou de `404 Not Found` para `401 Unauthorized`.
- `LoginHandler` agora recebe `IPasswordHasher` como dependência injetada.
- Testes expandidos com cenários específicos de validação de senha:
  - `Handle_ReturnsToken_WhenCredentialsAreValid`
  - `Handle_ReturnsUnauthorized_WhenPasswordIsInvalid`
  - `Handle_ReturnsUnauthorized_WhenUserDoesNotExist`
- Novos testes unitários para `PasswordHasher` em `PasswordHasherTests.cs` com cobertura de:
  - Hash production e diferentes hashes para mesma senha (salts aleatórios)
  - Verify com senha correta e incorreta
  - Edge cases: empty/null password e hash
  - Exceptions para entrada inválida

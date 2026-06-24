# Mini-spec: Registro de conta

Número: 12
Status: em andamento

## Problema

- O backend não possui fluxo de criação de conta para novos usuários.
- Sem endpoint de registro, o sistema depende de seed/manual para inserir usuários na base persistente.
- A ausência de cadastro bloqueia o ciclo completo de autenticação para uso real.

## Comportamento esperado

- Expor endpoint de registro para criar nova conta com e-mail e senha.
- Validar unicidade de e-mail antes de persistir.
- Persistir senha somente em formato de hash seguro.
- Retornar resposta padronizada de criação sem expor dados sensíveis.

## Superfícies afetadas

- Endpoints: novo `POST /auth/register`.
- Handlers: novo handler/use case de registro.
- Workers/Provedores: sem impacto direto.
- Integrações externas: sem obrigatoriedade nesta fase.

## Dados e persistência

- Entidade `UserEntity` deve ser criada com `Email` único e `PasswordHash`.
- Repositório de usuários deve oferecer operação de criação com validação de duplicidade.
- Compatível com a persistência durável atual (EF Core + SQLite) e preparada para PostgreSQL em fase futura.

## Contratos de API

- Endpoint: `POST /auth/register`.
- Request: `email`, `password`, `confirmPassword` (obrigatório).
- Response: confirmação de conta criada (sem retornar senha/hash/token).
- Códigos HTTP:
  - `201 Created`: conta criada com sucesso.
  - `400 Bad Request`: payload inválido.
  - `409 Conflict`: e-mail já cadastrado.

## Regras de validação

- `email` obrigatório em formato válido.
- `password` obrigatória com política mínima configurável:
  - tamanho mínimo de 8 caracteres;
  - pelo menos 1 letra maiúscula;
  - pelo menos 1 letra minúscula;
  - pelo menos 1 número;
  - pelo menos 1 caractere especial.
- `confirmPassword` é obrigatória e deve coincidir com `password`.
- Nunca registrar senha em logs.

## Critérios de aceite

- Cadastro com dados válidos cria usuário novo no repositório.
- Tentativa de cadastro com e-mail já existente retorna conflito.
- Senha nunca é persistida em texto puro.
- Resposta da API não expõe `PasswordHash`.

## Testes esperados

- Teste de sucesso no cadastro com payload válido.
- Teste de falha para e-mail duplicado.
- Teste de validação para e-mail inválido.
- Teste de validação para senha com menos de 8 caracteres.
- Teste de validação para senha sem letra maiúscula.
- Teste de validação para senha sem letra minúscula.
- Teste de validação para senha sem número.
- Teste de validação para senha sem caractere especial.
- Teste de validação para ausência de `confirmPassword`.
- Teste de validação para `confirmPassword` diferente de `password`.
- Teste garantindo persistência de hash (e não senha bruta).

## Fora de escopo

- Verificação de e-mail.
- Login social.
- Recuperação de conta por token/e-mail.
- Política avançada de senha (histórico, expiração, MFA).

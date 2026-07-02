# Mini-spec: Endpoint de redefinição de senha

Número: 24
Status: planejado
Origem: desdobramento funcional da Spec 06 (recover)

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- A recuperação de acesso terá endpoint dedicado, mas ainda não existe fluxo de redefinição orientado ao usuário final.
- Sem endpoint de reset, o usuário não consegue concluir o ciclo de recuperação com segurança.

## Objetivo

- Definir e implementar fluxo de redefinição de senha por endpoints dedicados e seguros.
- Permitir atualização de senha sem expor dados sensíveis e sem quebrar contratos atuais.
- Preparar o backend para integração real de e-mail com token de uso único.

## Interferência com mini-specs existentes

- Interfere com [Endpoint de recuperação de acesso](06-endpoint-auth-recover.md): esta spec é etapa subsequente e consome o fluxo iniciado no recover.
- Interfere com [Autenticação com validação de senha](../done/02-autenticacao-com-senha.md): deve manter compatibilidade com hashing PBKDF2 e validação de credenciais.
- Interfere com [Registro de conta](../done/12-registro-de-conta.md): deve reaproveitar política de senha já definida para novo segredo.
- Interfere com [Rollout de auditoria operacional no projeto](../done/23-rollout-de-auditoria-operacional-no-projeto.md): o fluxo de reset deve ser auditado como endpoint operacional.

## Comportamento esperado

- Expor contrato para validação de token de reset e submissão da nova senha.
- Criar endpoints de redefinição de senha com respostas claras de sucesso e erro.
- Garantir neutralidade e segurança nas mensagens de erro para não ampliar vetores de enumeração e abuso.

## Superfícies afetadas

- Endpoints: novos endpoints para confirmar token e redefinir senha.
- Handlers: use cases de validação de token e atualização de senha.
- Integrações externas: envio de e-mail com token de recuperação (quando disponível).

## Dados e persistência

- Criar mecanismo de token de reset de uso único com expiração.
- Persistir apenas dados necessários ao controle de validade, tentativa e consumo do token.
- Não persistir senha em texto puro; somente hash seguindo padrão vigente.

## Contratos de API (propostos)

- `POST /auth/reset/validate`: valida token de redefinição.
- `POST /auth/reset/confirm`: aplica nova senha para token válido.
- Códigos HTTP propostos:
  - `200 OK`: validação/aplicação concluída.
  - `422 Unprocessable Entity`: payload inválido.
  - `410 Gone`: token expirado ou já utilizado.
  - `429 Too Many Requests`: política antiabuso.

## Regras de validação

- `newPassword` deve cumprir política de senha vigente.
- `confirmPassword` deve ser obrigatório e igual a `newPassword`.
- Token deve ser válido, não expirado e de uso único.
- Endpoint deve possuir política antiabuso e proteção contra replay.
- Logs não devem expor token, senha ou segredo.

## Auditoria operacional

- Toda operação de reset deve entrar na trilha de auditoria operacional conforme Spec 23.
- Eventos devem registrar status, categoria, correlação e metadata sem conteúdo sensível.

## Critérios de aceite

- Usuário consegue redefinir senha via fluxo dedicado de forma segura.
- Token inválido, expirado ou já consumido não permite redefinição.
- A nova senha passa a ser exigida no login imediatamente após redefinição.
- O fluxo registra auditoria operacional e respeita políticas de segurança.

## Testes esperados

- Teste de validação de token válido/inválido/expirado/consumido.
- Teste de redefinição com payload válido e inválido (`422`).
- Teste de reuso de token bloqueado.
- Teste de login com nova senha após reset.
- Teste de auditoria operacional e sanitização de logs.
- Teste de política antiabuso.

## Fora de escopo

- MFA no fluxo de recuperação.
- Login social.
- Gestão avançada de dispositivos/sessões.

# Mini-spec: Endpoint de recuperação de acesso

Número: 06
Status: em andamento
Origem: [Issue #21](https://github.com/StephHoel/live-chat-bridge/issues/21)

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- Não existe endpoint dedicado para fluxo de recuperação de acesso.
- A ausência desse endpoint impede integração padronizada do frontend.

## Comportamento esperado

- Expor endpoint `POST /auth/recover/` para receber e-mail.
- A rota deve orquestrar requisição e delegar regra de negócio para serviço/use case.
- Resposta deve ser neutra e não revelar existência de conta.

## Superfícies afetadas

- Endpoints: rota de recover.
- Handlers: handler/use case de recuperação.
- Workers/Provedores: sem impacto.
- Integrações externas: provedor de envio de e-mail (ainda não implementado para produção).

## Dados e persistência

- Deve consultar usuário por e-mail sem expor resultado no contrato HTTP.
- Não persistir dados sensíveis em log.
- Em `Development` e `Test`, gerar token temporário de recuperação para uso de desenvolvimento/testes e incluir esse token no payload de `200 OK`.
- Em `Hmg` e `Production`, não retornar token no payload e manter mensagem operacional explícita de pendência de integração de e-mail.

## Contratos de API

- Request: `{ email: string }`.
- Response (`200 OK`) adota opção A com payload mínimo:
  - `message`: mensagem neutra de confirmação.
  - `temporaryResetToken`: token temporário de recuperação **somente** em `Development` e `Test`.
- Códigos HTTP:
  - `200 OK`: resposta neutra sempre que request for válido.
  - `422 Unprocessable Entity`: payload inválido.

## Regras de validação

- `email` obrigatório e em formato válido.
- Não retornar diferença entre usuário existente e inexistente.
- Considerar payload inválido (`422`) para: e-mail ausente, e-mail inválido, vazio, whitespace, formato inválido, `null`, JSON malformado, entre outros.
- Aplicar política antiabuso no endpoint em todos os ambientes, exceto `Test`.
- Em `Development` e `Test`, o payload `200` deve manter o mesmo formato para usuário existente e inexistente, preservando neutralidade.
- Política de logs por ambiente:
  - `Development` e `Test`: e-mail pode aparecer em texto puro.
  - `Hmg` e `Production`: e-mail deve ser mascarado.

## Critérios de aceite

- Endpoint sem regra de negócio acoplada.
- Serviço centraliza a lógica.
- Resposta neutra implementada conforme issue.
- Fluxo executa consulta real de usuário sem alterar neutralidade do contrato HTTP.
- Endpoint gera trilha de auditoria operacional conforme baseline da Spec 23.
- Contrato `200` usa opção A e inclui `temporaryResetToken` apenas em `Development` e `Test`.

## Testes esperados

- Teste de endpoint para payload válido.
- Teste para payload inválido com `422` nos cenários definidos.
- Teste garantindo resposta neutra para e-mail existente/inexistente.
- Teste do contrato `200` em `Development` e `Test` com `message` + `temporaryResetToken`.
- Teste do contrato `200` em `Hmg` e `Production` sem `temporaryResetToken`.
- Teste da política antiabuso habilitada em ambientes não `Test`.
- Teste da política de masking de e-mail por ambiente (`Development/Test` vs `Hmg/Production`).
- Teste de auditoria operacional para sucesso e falha no fluxo de recover.

## Fora de escopo

- Envio real de e-mail.
- Token de reset e tela de redefinição de senha.

## Interferência com mini-specs existentes

- Interfere com [Autenticação com validação de senha](../done/02-autenticacao-com-senha.md): a recuperação não pode depender de descriptografia de senha atual, pois o armazenamento usa hash PBKDF2 irreversível.
- Interfere com [Registro de conta](../done/12-registro-de-conta.md): deve preservar regra de não exposição de segredo e não alterar política de senha.
- Interfere com [Rollout de auditoria operacional no projeto](../done/23-rollout-de-auditoria-operacional-no-projeto.md): novo endpoint deve entrar na trilha de auditoria operacional.

## Decisões registradas para implementação

- Padrão de rota: `POST /auth/recover/`.
- Payload inválido retorna `422` nos casos definidos nesta spec.
- Consulta real de usuário é obrigatória, mantendo resposta neutra.
- `200 OK` segue opção A com payload mínimo.
- Em `Development` e `Test`, gerar e retornar `temporaryResetToken` no payload para validação do fluxo.
- Em `Hmg` e `Production`, retornar mensagem de pendência de integração de e-mail para recuperação.
- Recover entra na trilha de auditoria operacional como qualquer nova implementação.
- Política antiabuso ativa em todos os ambientes, exceto `Test`.
- Logging de e-mail em recover por ambiente:
  - texto puro em `Development` e `Test`;
  - mascarado em `Hmg` e `Production`.

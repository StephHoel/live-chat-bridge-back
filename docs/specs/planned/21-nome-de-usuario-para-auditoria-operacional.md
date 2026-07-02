# Mini-spec: Nome de usuário para auditoria operacional

Número: 21
Status: planejado

## Problema

- A tabela `Users` ainda não possui um campo de nome de exibição.
- A auditoria operacional em `LiveSettings.UpdatedByUser` usa e-mail do usuário autenticado nesta fase.
- O e-mail funciona como identificador, mas não é o melhor valor para leitura operacional futura.

## Objetivo

- Adicionar nome de exibição ao domínio de usuário autenticável.
- Permitir que auditorias operacionais persistam nome do usuário em vez do e-mail quando esse dado existir.
- Alterar a evolução do contrato de `ActorUser` definido na Spec 23 (estado atual: e-mail/fallback técnico) para suportar nome de exibição quando aplicável.

## Interferência com mini-specs existentes

- Interfere com [docs/specs/active/23-rollout-de-auditoria-operacional-no-projeto.md](../active/23-rollout-de-auditoria-operacional-no-projeto.md): esta spec altera a estratégia de evolução do ator de auditoria (`ActorUser`) explicitada como estado atual na Spec 23.

## Superfícies afetadas

- Persistência: tabela `Users`.
- Endpoints/handlers de autenticação e registro.
- Superfícies que hoje gravam `UpdatedByUser` com e-mail.

## Dados e persistência

- Adicionar coluna de nome em `Users`.
- Definir estratégia de retrocompatibilidade para usuários já existentes.
- Avaliar migração de valores existentes em `UpdatedByUser`.

## Critérios de aceite

- `Users` passa a suportar nome de exibição.
- Fluxos de auditoria operacional podem utilizar nome no lugar do e-mail.
- A solução permanece compatível com SQLite atual e futura migração para PostgreSQL.

## Fora de escopo

- Perfil completo de usuário.
- Preferências visuais.
- Alterações em permissões/autorização.

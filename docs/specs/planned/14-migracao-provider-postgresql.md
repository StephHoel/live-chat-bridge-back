# Mini-spec: Migração de provider de banco para PostgreSQL

Número: 14
Status: planejado

## Problema

- A persistência local atual usa SQLite, que é adequado para desenvolvimento mas não para produção.
- PostgreSQL foi definido como provider de banco online ainda na fase de planejamento da spec 03.
- Sem a troca de provider, o sistema não pode operar em ambiente de produção gerenciado.

## Comportamento esperado

- A aplicação deve poder conectar-se a um PostgreSQL remoto mediante configuração.
- Nenhuma alteração de contrato de domínio ou de Application é necessária — a troca ocorre exclusivamente na camada de Infrastructure e na configuração.
- Migrations existentes devem ser aplicadas sobre o banco PostgreSQL alvo antes da primeira execução.
- O banco SQLite permanece funcional para desenvolvimento local sem nenhuma mudança no código de domínio ou handlers.

## Superfícies afetadas

- Endpoints: nenhuma mudança.
- Handlers: nenhuma mudança.
- Workers/Provedores: nenhuma mudança.
- Integrações externas: PostgreSQL como provider de banco gerenciado.
- Infraestrutura: `LcbDbContext`, registro de DI em `DependencyInjection.cs`, `appsettings.json` e `appsettings.*.json`.
- Projetos afetados: `LCB.Infrastructure`, `LCB.Api`, `LCB.UnitTest` (inclusão de testes de integração contra PostgreSQL).

## Dados e persistência

- Schema e migrations versionadas permanecem os mesmos — EF Core é responsável por aplicá-los no provider alvo.
- Validar compatibilidade de tipos entre SQLite e PostgreSQL nas três tabelas (`Users`, `Queues`, `ChatMessages`):
  - `UNIQUEIDENTIFIER` → `uuid` em PostgreSQL.
  - `TEXT` → `text` ou `varchar` com tamanho limitado.
  - `DATETIME` → `timestamp with time zone` (UTC).
  - `BIT` / `INTEGER` para booleanos → `boolean`.
  - Enum como string: verificar collation e case-sensitivity.
- Índices existentes devem ser validados no PostgreSQL (comportamento de índice único pode diferir).
- `Database:Provider` em configuração controla qual provider é carregado.

## Contratos de API

- Request: sem mudanças.
- Response: sem mudanças.
- Códigos HTTP: manter os atuais. Erros de conexão com o banco continuam retornando `500`.

## Regras de validação

- A troca de provider não pode alterar comportamento observável pelos clientes da API.
- Migrations devem ser aplicadas via `dotnet ef database update` ou equivalente antes da subida da aplicação — ou pelo mecanismo de `Migrate()` no startup, a definir.
- Configuração de string de conexão nunca deve ser commitada com credenciais reais.

## Critérios de aceite

- Aplicação sobe e responde normalmente apontada para PostgreSQL.
- Migrations aplicam-se sobre banco PostgreSQL vazio sem erros.
- Login e ingestão de mensagens funcionam com banco PostgreSQL.
- Ambiente de desenvolvimento com SQLite não é afetado pela mudança.
- Strings de conexão sensíveis são gerenciadas por variável de ambiente ou secrets, nunca em appsettings versionado.

## Testes esperados

- Testes de integração dos repositórios executando contra PostgreSQL (ambiente de CI ou container Docker).
- Teste de aplicação de migration em banco vazio PostgreSQL.
- Testes de smoke para login e ingestão de mensagem com provider PostgreSQL.
- Testes unitários existentes continuam passando (não dependem de provider específico).

## Fora de escopo

- Replicação, failover ou alta disponibilidade do banco.
- Connection pooling avançado (PgBouncer ou similar).
- Backup e restore automatizados.
- Troca dinâmica de provider em runtime sem reinício.
- Qualquer mudança em contratos de domínio, handlers ou endpoints.

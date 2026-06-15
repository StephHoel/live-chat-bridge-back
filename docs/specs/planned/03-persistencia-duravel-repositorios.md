# Mini-spec: Persistência durável para repositórios

Número: 03
Status: planejado

## Problema

- O sistema perde mensagens, fila e usuários ao reiniciar porque a persistência é somente em memória.
- Não há base para operação confiável entre reinícios.

## Comportamento esperado

- Repositórios de mensagens, fila e usuários devem persistir dados de forma durável.
- Reinício da aplicação não pode apagar estado de negócio essencial.
- Camada de aplicação deve continuar dependente de contratos do domínio.

## Superfícies afetadas

- Endpoints: `POST /auth/login`, `POST /messages/ingest` e demais que dependam de repositórios.
- Handlers: `LoginHandler`, `MessageIngestHandler` e handlers que acessam repositórios.
- Workers/Provedores: impactados indiretamente quando persistirem mensagens.
- Integrações externas: banco de dados a definir.

## Dados e persistência

- Mapear entidades de domínio para estrutura persistida (`User`, `Queue`, `ChatMessage`).
- Definir estratégia de migração da base atual (cold start ou seed controlado).
- Definir índices mínimos: e-mail de usuário, chave de idempotência, usuário em fila.

## Contratos de API

- Request: sem mudanças obrigatórias.
- Response: sem mudanças obrigatórias.
- Códigos HTTP: manter códigos atuais, exceto falhas operacionais de infraestrutura (`500`).

## Regras de validação

- Gravação deve ser transacional para operações críticas de ingestão.
- Falhas de persistência devem ser logadas com correlação.

## Critérios de aceite

- Dados sobrevivem a reinício da API.
- Login e ingestão continuam funcionando com repositórios persistentes.
- Consultas por idempotência e fila mantêm comportamento funcional.

## Testes esperados

- Testes de integração para CRUD básico de usuários, fila e mensagens.
- Teste de reinicialização validando durabilidade.
- Testes de erro de infraestrutura com fallback de resposta.

## Fora de escopo

- Estratégias avançadas de replicação/alta disponibilidade.
- Dashboard administrativo.

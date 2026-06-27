# Mini-spec: Configuração persistida de live e usernames por plataforma

Número: 19
Status: planejado

## Problema

- Os usernames das redes sociais de live hoje dependem de configuração estática e não de uma configuração operacional gerenciada pelo front.
- O sistema não possui endpoint próprio para leitura e atualização dessas configurações.
- Não existe persistência dedicada para armazenar usernames e demais configurações operacionais de live de forma durável.

## Comportamento esperado

- Criar uma configuração operacional persistida para as plataformas de live suportadas.
- Permitir que o front consulte a configuração atual.
- Permitir que o front atualize os usernames das redes sociais por endpoint protegido.
- Preparar a estrutura para expansão futura de outras configurações operacionais sem depender de `appsettings` como fonte principal.

## Decisão de modelagem

- A configuração será tratada como configuração global do sistema nesta fase, e não como preferência individual por usuário.
- A recomendação é usar uma tabela por plataforma, em vez de um único blob JSON, para manter validação, índices e evolução de schema mais previsíveis.

## Superfícies afetadas

- Endpoints: novo endpoint de leitura e atualização de configuração de live.
- Handlers: use case de consulta e use case de atualização da configuração.
- Workers/Provedores: dependem dessa configuração para obter usernames ao iniciar listeners.
- Integrações externas: front autenticado que administra a operação.

## Dados e persistência

- Criar nova tabela `LivePlatformSettings` com uma linha por plataforma suportada.
- Campos mínimos sugeridos:
  - `Id` (Guid)
  - `Platform` (string ou enum persistido como string)
  - `Username` (string, opcional enquanto a plataforma não estiver configurada)
  - `CreatedAtUtc` (DateTime)
  - `UpdatedAtUtc` (DateTime)
  - `UpdatedByUser` (string)
- Índices recomendados:
  - `Platform` único
  - `UpdatedAtUtc`
- A tabela deve nascer compatível com SQLite atual e com a futura migração para PostgreSQL.

## Contratos de API

- `GET /config/live`: retorna usernames configurados por plataforma.
- `PUT /config/live`: atualiza usernames configurados por plataforma.

- Exemplo de request para `PUT /config/live`:

```json
{
  "tiktokUsername": "canal_tiktok",
  "twitchUsername": "canal_twitch",
  "youtubeUsername": "canal_youtube"
}
```

- Exemplo de response para `GET /config/live`:

```json
{
  "tiktokUsername": "canal_tiktok",
  "twitchUsername": "canal_twitch",
  "youtubeUsername": "canal_youtube"
}
```

- Códigos HTTP esperados:
  - `200 OK`: leitura ou atualização concluída.
  - `400 Bad Request`: payload inválido.
  - `401 Unauthorized`: front não autenticado.
  - `409 Conflict`: conflito de consistência operacional, se aplicável.

## Regras de validação

- O endpoint deve ser protegido por autenticação.
- Usernames devem ser normalizados com `trim` e remoção opcional de prefixos incompatíveis com o provider, quando a regra já for conhecida.
- O backend não deve aceitar valores arbitrários que inviabilizem o listener da plataforma.
- Atualização parcial pode ser permitida, desde que a semântica do contrato seja explícita.
- Toda atualização deve registrar `UpdatedByUser` a partir do contexto autenticado.

## Critérios de aceite

- Existe persistência durável para usernames por plataforma.
- O front consegue consultar a configuração atual por endpoint protegido.
- O front consegue atualizar usernames por endpoint protegido.
- O worker passa a depender dessa configuração persistida em vez de depender exclusivamente de `appsettings`.
- A solução permanece compatível com a futura migração para PostgreSQL.

## Testes esperados

- Teste de repositório para criação e atualização das configurações por plataforma.
- Teste de endpoint para leitura da configuração atual.
- Teste de endpoint para atualização válida.
- Teste de autenticação obrigatória nos endpoints.
- Teste de normalização de usernames.
- Teste de compatibilidade da migration em banco vazio.

## Fora de escopo

- Preferências individuais por usuário autenticado.
- Versionamento histórico completo das configurações.
- Edição de flags de start/stop do worker neste endpoint.

# Mini-spec: Supressão de comandos SQL e parâmetros do EF Core no log

Número: 31
Status: planejado

## Problema

- O EF Core registra, no nível `Information` da categoria `Microsoft.EntityFrameworkCore.Database.Command`, o texto completo do comando SQL executado (`INSERT`, `UPDATE`, `SELECT`, etc.) e a lista de parâmetros com nome, tipo (`DbType`) e tamanho (`Size`), como no exemplo:

  ```plain
  [Command] Executed DbCommand (1ms) [Parameters=[@p0='?' (DbType = Guid), @p1='?' (Size = 26), @p2='?' (Size = 14), @p3='?' (DbType = DateTime), @p4='?' (Size = 242), @p5='?' (Size = 16), @p6='?' (Size = 4)], CommandType='Text', CommandTimeout='30']
  INSERT INTO "AuditLogs" ("Id", "Action", "ActorUser", "CreatedAtUtc", "MetadataJson", "Resource", "Status")
  VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6);
  ```

- Nenhuma configuração atual em `appsettings.json`/`appsettings.Development.json` sobrescreve o nível de log dessa categoria (`Logging:LogLevel:Default = Information` se aplica a ela por padrão), então toda a estrutura de comando (nomes de tabela, colunas, tipos e tamanhos de campo) aparece nos logs em todos os ambientes.
- `EnableSensitiveDataLogging` não está habilitado em `LCB.Infrastructure/DI/DependencyInjection.cs`, então os valores literais dos parâmetros já não vazam (aparecem como `'?'`), mas a estrutura da query (schema, nomes de coluna/tabela, quantidade e tipo de parâmetros) ainda é exposta em texto livre no log, o que facilita reconhecimento de schema por quem tiver acesso aos logs e é considerado ruído/exposição desnecessária de detalhes internos de persistência.
- `TemplateLogger.IsEnabled` (`src/LCB.Api/Logging/TemplateLogger.cs`) sempre retorna `true`, então a filtragem por nível de log depende inteiramente das regras vinculadas via `logging.AddConfiguration(configuration.GetSection("Logging"))` em `LoggingDependencies.AddLogging`; qualquer categoria sem override explícito herda `Logging:LogLevel:Default`.

## Comportamento esperado

- Comandos SQL e metadados de parâmetros do EF Core (categoria `Microsoft.EntityFrameworkCore.Database.Command`) não devem mais aparecer no log dos seguintes ambientes: `Development`, `Hmg`, `Production`, `Test`.
- Erros reais do EF Core (ex.: falha de conexão, violação de constraint) continuam sendo logados normalmente, pois pertencem a outras categorias/níveis (`Warning`/`Error`) não afetados por esta mudança.
- Nenhum outro comportamento de log (correlação por request, formatação do `TemplateLogger`, demais categorias) é alterado.
- Caso um desenvolvedor precise depurar comandos SQL localmente, deve existir uma forma documentada de reativar o log de comandos apenas em ambiente local, sem reintroduzir o vazamento por padrão (ex.: override manual e temporário do `LogLevel` em `appsettings.Development.json`, nunca commitado como padrão do ambiente).

## Superfícies afetadas

- Endpoints: nenhum diretamente; efeito é transversal a qualquer operação que use `LcbDbContext`.
- Handlers: nenhum.
- Workers/Provedores: nenhum diretamente, mas o worker também usa repositórios EF Core e é beneficiado pela supressão.
- Configuração: `src/LCB.Api/appsettings.json`, `src/LCB.Api/appsettings.Development.json` (seção `Logging:LogLevel`).
- Infraestrutura de logging: `LCB.Api/DependencyInjection/LoggingDependencies.cs` (validar que `AddConfiguration` aplica corretamente o filtro por categoria mesmo com `TemplateLogger.IsEnabled` sempre `true`).

## Dados e persistência

- Nenhuma alteração de schema, entidade ou repositório.
- Nenhuma alteração de comportamento de persistência; a mudança afeta apenas verbosidade de log.

## Contratos de API

- Nenhum contrato de request/response é alterado.
- Nenhum código HTTP é alterado.

## Regras de validação

- Não aplicável (mudança de configuração de logging, sem regra de negócio nova).

## Critérios de aceite

- Ao executar qualquer operação que dispare comando SQL via EF Core (ex.: login, ingestão de mensagem, escrita de auditoria), o log não deve conter linhas iniciadas por `Executed DbCommand` nem o texto do comando SQL subsequente, em nenhum ambiente configurado (`appsettings.json` e `appsettings.Development.json`).
- A categoria `Microsoft.EntityFrameworkCore.Database.Command` deve ter nível mínimo `Warning` (ou mais restritivo) em `Logging:LogLevel` de `appsettings.json`, garantindo o comportamento também em `Hmg`/`Production` que herdam desse arquivo base.
- `appsettings.Development.json` deve manter o mesmo nível mínimo (`Warning` ou mais restritivo) para a mesma categoria, para não reintroduzir o vazamento apenas em desenvolvimento.
- Erros e warnings genuínos do EF Core (categoria de nível `Warning`/`Error`) continuam aparecendo no log normalmente.
- Nenhuma outra categoria de log existente perde visibilidade além da categoria alvo desta spec.

## Testes esperados

- Teste de integração (ou unitário direcionado ao provider de log) que force uma operação de escrita via `LcbDbContext` e verifique que nenhuma entrada de log com a categoria `Microsoft.EntityFrameworkCore.Database.Command` em nível `Information` é emitida pelo `TemplateLoggerProvider`.
- Validação manual/documentada: executar a aplicação localmente, disparar um fluxo com escrita no banco (ex.: `POST /auth/register` ou ingestão de mensagem) e confirmar ausência de `Executed DbCommand` na saída de console.

## Fora de escopo

- Revisão geral de todas as categorias de log da aplicação (fora do escopo desta spec, que é focada apenas na categoria de comandos do EF Core).
- Mudança na estratégia de `TemplateLogger`/`TemplateLoggerProvider` além do necessário para validar a filtragem por categoria.
- Habilitação ou desabilitação de `EnableSensitiveDataLogging` (já está desabilitado e deve permanecer assim; não é objeto desta spec).
- Mascaramento adicional de dados sensíveis em outras categorias de log (ex.: auditoria operacional), já tratado nas Specs 15/23.

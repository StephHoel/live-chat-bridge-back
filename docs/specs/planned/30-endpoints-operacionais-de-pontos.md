# Mini-spec: Endpoints operacionais de pontos

Número: 30
Status: planejado
Origem: desdobramento das decisões das Specs 08 e 09

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- Ainda não existem endpoints operacionais de pontos para consulta de saldo e limpeza manual.
- A observabilidade/auditoria operacional de ações de pontos ainda não está definida no contrato HTTP.

## Comportamento esperado

- Expor endpoint operacional para consulta de pontos dos espectadores do streamer.
- A consulta deve retornar, por padrão, os 10 primeiros saldos (do maior para o menor) considerando todos os canais ativos na sessão do worker do streamer.
- A consulta deve suportar paginação e ajuste de quantidade por página para permitir visualização de mais de 10 itens por vez no frontend.
- O parâmetro `pageSize` deve aceitar apenas os valores `10`, `25`, `50` ou `100`.
- Expor endpoint operacional para limpeza global de saldo do streamer.
- Registrar auditoria operacional para ações de consulta e limpeza manual de pontos via HTTP.
- Manter compatibilidade com regras da Spec 08, com o caso de uso/evento da Spec 09 e com o catálogo da Spec 29.

## Superfícies afetadas

- Endpoints: novos endpoints de pontos para frontend operacional.
- Handlers: casos de uso de consulta e limpeza global de pontos do streamer.
- Workers/Provedores: sem alteração obrigatória de contrato nesta mini-spec.
- Integrações externas: sem alteração obrigatória.

## Dados e persistência

- Reutilizar modelos e repositórios definidos na Spec 08 (`PointsBalance` e `PointsTransaction`).
- A consulta operacional deve considerar apenas registros com `isActive = true`.
- Operações manuais (`clear`) devem registrar trilha transacional com contexto (`provider + channelId + userId`).
- A limpeza global deve aplicar `isActive = false` para todos os registros de pontos dos providers ativos do streamer.

## Contratos de API

- Definir endpoint de consulta paginada de pontos do streamer autenticado.
  - GET /points/all
  - Ordenação: `points` decrescente.
  - Retorno padrão: 10 itens por página.
  - Filtros de paginação: `page` e `pageSize`.
  - Valores permitidos para `pageSize`: `10`, `25`, `50`, `100`.
  - `pageSize` fora da lista permitida deve retornar erro de payload (`422 Unprocessable Entity`).
  - Escopo da consulta: todos os usuários dos canais ativos do streamer na sessão do worker.
- Definir endpoint operacional para limpeza global de pontos do streamer autenticado.
  - POST /points/clear
  - Escopo da limpeza: todos os providers/canais ativos do streamer.
- Manter envelope `Result<T>` em todos os responses (sucesso e erro).
- Códigos de erro:
  - `422 Unprocessable Entity` para erro de requisição/validação.
  - `400 Bad Request` para falha geral de execução.

## Regras de validação

- Providers válidos para consulta/limpeza devem ser apenas os canais ativos da sessão do worker do streamer autenticado.
- Consulta operacional deve retornar apenas registros com `isActive = true`.
- `pageSize` inválido (diferente de `10`, `25`, `50` ou `100`) deve falhar com erro de payload (`422`).
- Limpeza deve desativar pontos dos usuários, marcando `isActive = false` para torná-los inacessíveis na consulta.
- Todas as operações manuais devem gerar transação de pontos e auditoria operacional.

## Critérios de aceite

- Front consegue consultar saldo de pontos dos espectadores do usuário logado considerando todos os canais ativos.
- Front recebe por padrão os 10 maiores saldos e consegue ajustar paginação/quantidade por página.
- Front consegue executar limpeza global dos pontos dos espectadores do usuário logado.
- Consultas e alterações operacionais de pontos devem ser auditadas.

## Testes esperados

- Testes de integração dos endpoints operacionais de pontos.
- Testes de validação para erro `422` em consulta e limpeza.
- Testes de validação para `pageSize` inválido retornando erro de payload (`422`).
- Testes de falha geral com retorno `400`.
- Testes de paginação e ordenação descrescente por pontos (incluindo default de 10 itens).
- Testes de consistência de saldo em limpeza manual.
- Testes para garantir que consulta retorna apenas `isActive = true`.
- Testes de auditoria para consulta e limpeza de pontos.

## Fora de escopo

- Inclusão de novos `integrationType` em runtime sem deploy.
- Mudança da regra de atualização atômica de saldo definida na Spec 08.
- Transporte SSE/WebSocket para notificações de pontos no frontend.

## Dependências

- Depende da infraestrutura de domínio/repositório definida na Spec 08.
- Depende do caso de uso/evento de pontuação definido na Spec 09.
- Depende do catálogo de `integrationType` por streamer/contexto definido na Spec 29.

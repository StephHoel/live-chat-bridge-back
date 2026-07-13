# Mini-spec: Catálogo de integrationType

Número: 29
Status: implementado
Origem: desdobramento das decisões das Specs 08 e 09

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- Ainda não existe catálogo persistido de `integrationType` por streamer para definição de pontuação via frontend.

## Comportamento esperado

- Criar tabela/catálogo de `integrationType` com configuração de pontuação por streamer/contexto.
- Disponibilizar serviço/repositório para leitura eficiente do catálogo no fluxo de pontuação.
- Garantir fallback seguro quando não houver regra configurada para combinação `provider + integrationType`.
- Manter compatibilidade com regras da Spec 08 e use case/evento da Spec 09.

## Superfícies afetadas

- Endpoints: sem criação obrigatória de novos endpoints nesta mini-spec.
- Handlers: casos de uso que consultam catálogo para cálculo de delta.
- Workers/Provedores: sem alteração obrigatória de contrato nesta mini-spec.
- Integrações externas: sem alteração obrigatória.

## Dados e persistência

- Criar entidade persistente para catálogo de `integrationType` por streamer/contexto de pontos.
- O catálogo deve permitir valores iniciais: `message`, `like`.
- Novos tipos continuam permitidos apenas via programação e novo deploy.
- A configuração deve ser aplicável no cálculo de delta sem quebrar fallback seguro da Spec 08.

## Contratos de API

- Request: não define rota nova obrigatória por si só.
- Response: não define rota nova obrigatória por si só.
- Códigos HTTP: sem impacto direto nesta mini-spec.

## Regras de validação

- `integrationType` desconhecido não deve adicionar pontos.
- Plataforma/provider desconhecido deve ser ignorado sem crédito de pontos.

## Critérios de aceite

- Catálogo persistido por streamer/contexto pode ser consultado pela regra de pontuação.
- Quando regra não existir para a combinação recebida, o delta aplicado é seguro (`0`).
- Regras configuradas para `message` e `like` passam a valer no fluxo de pontuação.

## Testes esperados

- Testes de persistência e consulta do catálogo por streamer/contexto.
- Testes de validação para fallback seguro em `integrationType` e provider desconhecidos.

## Fora de escopo

- Inclusão de novos `integrationType` em runtime sem deploy.
- Mudança da regra de atualização atômica de saldo definida na Spec 08.
- Endpoints operacionais de consulta, crédito/débito e limpeza manual de pontos (cobertos na Spec 30).

## Dependências

- Depende da infraestrutura de domínio/repositório definida na Spec 08.
  - A Spec 08 entregou a política estática em `PointsPolicy` (deltas fixos por provider/integrationType) como fallback seguro até esta spec ser implementada. A Spec 29 substituirá essa política estática por configuração persistida por streamer.
- Complementa a Spec 09, que cobre caso de uso transacional de pontuação e evento `points_updated`.
- Serve de base para a Spec 30, que expõe operações HTTP de pontos para uso operacional.

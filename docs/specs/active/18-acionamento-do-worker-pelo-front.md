# Mini-spec: Acionamento do worker pelo front

Número: 18
Status: em andamento

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- Hoje o worker inicia junto com a aplicação e passa a operar sem um gesto explícito do front.
- Isso dificulta controle operacional, previsibilidade de início e parada, e a separação entre API disponível e worker efetivamente ativo.
- Falta um contrato claro para o front decidir quando o worker deve começar a processar eventos e quais listeners de redes sociais devem ser ativados em cada sessão.

## Objetivo

- Permitir que o worker só funcione após um acionamento explícito vindo do front.
- Definir um contrato simples e seguro para iniciar, consultar e, se necessário, interromper o worker.
- Permitir que o front informe, no start, quais redes sociais entrarão em live naquele momento por meio de flags booleanas.
- Garantir que start/stop/status atuem somente sobre a instância lógica do usuário autenticado, sem efeito global sobre outras instâncias ativas de outros usuários.
- Preservar o comportamento atual da API HTTP fora desse controle operacional.

## Opções de acionamento avaliadas

1. `POST /worker/start` e `POST /worker/stop` protegidos por autenticação.
2. Um endpoint único de comando operacional, por exemplo `POST /worker/control`, com ação no corpo (`start` / `stop`).
3. Canal em tempo real com SignalR/WebSocket para comando do front ao backend.

## Recomendação

- Preferir `POST /worker/start` e `POST /worker/stop` como contrato explícito e fácil de testar.
- Complementar com `GET /worker/status` para o front exibir se o worker está ativo, parado ou em transição.
- O payload de `POST /worker/start` deve indicar explicitamente quais listeners serão ativados (`tiktok`, `twitch`, `youtube`).
- Os usernames das plataformas não devem ser enviados no start; eles devem vir da configuração persistida do banco.
- O alvo operacional dos endpoints deve ser sempre a instância do usuário autenticado (derivada do token), sem parâmetro de usuário no payload para evitar controle cruzado.
- Manter o acionamento restrito a um front autenticado, sem início automático no boot.

## Comportamento esperado

- O worker inicia em estado inativo.
- O front envia um comando explícito para habilitar o worker, informando quais plataformas devem ser ativadas.
- O backend registra e expõe o estado operacional atual do worker.
- O worker pode ser interrompido por comando explícito, sem derrubar a API HTTP inteira.
- O worker só tenta conectar listeners para plataformas marcadas como `true` no payload de start.
- O backend valida se cada plataforma ativada possui username configurado antes de abrir o listener correspondente.
- Comandos repetidos de start/stop devem ser idempotentes ou responder com estado atual.
- `POST /worker/start`, `POST /worker/stop` e `GET /worker/status` devem refletir apenas o contexto do usuário autenticado que fez a chamada.
- O start/stop de um usuário não pode iniciar, parar ou alterar status de workers de outros usuários ativos.

## Superfícies afetadas

- Endpoints: novos endpoints de controle do worker e endpoint de status.
- Handlers: orquestração de start/stop/status.
- Workers/Provedores: `ChatWorker` e possivelmente serviços auxiliares de ciclo de vida.
- Integrações externas: front da aplicação ou painel administrativo.
- Dependências funcionais: configuração persistida de usernames por plataforma.

## Dados e persistência

- O estado operacional do worker pode permanecer em memória nesta fase, desde que o comportamento fique explícito.
- O estado operacional em memória deve ser isolado por usuário (chaveado por `UserId` autenticado), e não compartilhado como estado global único.
- Se persistido, registrar o último estado conhecido e o horário da transição.
- A seleção de plataformas ativas no comando de start não deve exigir novo username no payload; o backend deve ler os usernames persistidos para cada rede habilitada.
- Não persistir payloads sensíveis de comando.

## Dependências e interferências conhecidas

- Esta mini-spec depende da existência de configuração persistida de usernames por plataforma, proposta na Spec 19.
- Esta mini-spec interfere em [docs/specs/done/05-processamento-real-chat-worker.md](../done/05-processamento-real-chat-worker.md), pois o fluxo de execução do worker deixa de ser implicitamente único/global e passa a exigir controle e isolamento por usuário/sessão.
- Esta mini-spec complementa a proteção já definida em [docs/specs/done/11-seguranca-basica-ingest-token-header.md](../done/11-seguranca-basica-ingest-token-header.md).
- Esta mini-spec altera o escopo originalmente proposto da própria Spec 18, substituindo um start genérico por um start com seleção explícita de listeners por rede.

## Contratos de API

- `POST /worker/start`: habilita o worker com seleção explícita de listeners.
- `POST /worker/stop`: interrompe o worker.
- `GET /worker/status`: retorna o estado atual do worker.
- O contexto-alvo dos três endpoints é sempre o usuário autenticado da requisição (`UserId` no token), sem aceitar `userId` no request para start/stop.
- Exemplo de request para `POST /worker/start`:

```cs
public class WorkerStartRequest
{
    public bool TikTok { get; set; }
    public bool Twitch { get; set; }
    public bool YouTube { get; set; }
}
```

- Exemplo de response para `GET /worker/status`:

```cs
public class WorkerStatusResponse
{
    public StateEnum State { get; set; }
    public bool TikTok { get; set; }
    public bool Twitch { get; set; }
    public bool YouTube { get; set; }
}
```

- Códigos HTTP esperados:
  - `200 OK`: comando executado ou status retornado.
  - `401 Unauthorized`: front não autenticado.
  - `400 Bad Request`: payload inválido ou nenhuma plataforma habilitada.
  - `409 Conflict`: comando incompatível com o estado atual ou tentativa de start com plataforma ativa sem username configurado.
  - `503 Service Unavailable`: worker indisponível por falha operacional.

## Regras de validação

- O front precisa estar autenticado para acionar o worker.
- O backend deve derivar o usuário-alvo exclusivamente do token autenticado e rejeitar qualquer tentativa de controle de instância que não pertença ao chamador.
- Start só pode ocorrer a partir de um estado inativo, salvo política explícita de idempotência para comando repetido.
- Stop só pode ocorrer a partir de um estado ativo.
- O worker não deve aceitar processamento antes do comando de start.
- Pelo menos uma plataforma deve ser enviada como `true` no comando de start.
- Toda plataforma marcada como `true` no start deve possuir username persistido válido.
- A ausência de username configurado para uma plataforma ativada deve impedir o start completo, evitando estado parcial silencioso.
- Estados intermediários devem ser bem definidos para evitar corrida entre início e consumo.

## Critérios de aceite

- O worker não processa mensagens até receber acionamento explícito do front.
- O front consegue iniciar o worker informando as plataformas ativas e consultar seu status.
- O front consegue interromper o worker sem afetar a API HTTP principal.
- O backend ativa apenas os listeners das plataformas marcadas como `true`.
- O backend bloqueia start inconsistente quando uma plataforma ativa não possui username configurado.
- Chamadas repetidas de start/stop não quebram o estado operacional.
- O start/stop/status de um usuário autenticado não altera o estado operacional de instâncias ativas de outros usuários.

## Testes esperados

- Teste de start do worker a partir de estado inativo com uma plataforma válida.
- Teste de stop do worker a partir de estado ativo.
- Teste de status refletindo transição correta.
- Teste de start/stop redundante.
- Teste de bloqueio quando o front não estiver autenticado.
- Teste de validação para payload sem nenhuma plataforma ativa.
- Teste de bloqueio de start quando plataforma ativa não possui username configurado.
- Teste de ativação seletiva de listeners por plataforma.
- Teste de isolamento por usuário: start/stop/status para usuário A não impacta o estado da instância do usuário B.

## Fora de escopo

- Orquestração distribuída entre múltiplas instâncias.
- Escalonamento automático do worker.
- Fila externa para comandos operacionais.
- Edição de usernames das plataformas no mesmo endpoint de start.

# Mini-spec: Login retorna usernames de live para bootstrap do front

Número: 20
Status: planejado

## Diretriz transversal de concorrência

- O sistema deve estar apto a operar com N usuários conectados simultaneamente.
- Esta mini-spec deve considerar execução concorrente de múltiplos workers/listeners, com isolamento por usuário (um worker lógico por usuário/sessão ativa).
- O desenho técnico não deve assumir worker único global como premissa obrigatória.

## Problema

- O login hoje retorna apenas o token JWT.
- Após autenticar, o front ainda precisa conhecer os usernames atuais das plataformas de live para montar a tela e preparar o acionamento operacional.
- Sem esse retorno, o front depende de uma chamada adicional imediata para bootstrap básico da operação.

## Comportamento esperado

- Alterar o contrato de `POST /auth/login` para retornar, além do token, os usernames de live atualmente configurados.
- O login deve continuar validando credenciais exatamente como hoje, sem relaxar nenhuma regra de segurança.
- Quando uma plataforma ainda não possuir username configurado, o login deve retornar valor nulo ou vazio de forma explícita, sem falhar a autenticação por isso.

## Superfícies afetadas

- Endpoints: `POST /auth/login`.
- Handlers: `LoginHandler` e possível serviço auxiliar de composição da resposta.
- Workers/Provedores: sem impacto direto.
- Integrações externas: front autenticado que usa o payload de login para bootstrap inicial.

## Dependências e interferências conhecidas

- Esta mini-spec depende da configuração persistida proposta na Spec 19.
- Esta mini-spec altera o contrato implementado na [docs/specs/done/02-autenticacao-com-senha.md](../done/02-autenticacao-com-senha.md), que hoje retorna apenas token.
- A autenticação e a emissão do JWT continuam regidas pela implementação já consolidada; a mudança é exclusivamente de composição do response em sucesso.

## Dados e persistência

- Nenhuma nova tabela é criada nesta mini-spec.
- Os usernames retornados devem ser lidos da persistência de configuração de live.
- Na ausência de configuração, o login não deve falhar; deve apenas devolver valores vazios ou nulos para as plataformas não configuradas.

## Contratos de API

- Endpoint: `POST /auth/login`.
- Request: mantém `email` e `password`.
- Response de sucesso passa a incluir `token` e usernames de live.

- Exemplo de response:

```json
{
  "success": true,
  "data": {
    "token": "jwt-token",
    "liveUsernames": {
      "tiktok": "canal_tiktok",
      "twitch": "canal_twitch",
      "youtube": "canal_youtube"
    }
  }
}
```

- Códigos HTTP esperados:
  - `200 OK`: credenciais válidas e payload de bootstrap retornado.
  - `401 Unauthorized`: credenciais inválidas.
  - `500 Internal Server Error`: falha inesperada na composição do response.

## Regras de validação

- O login não pode expor dados sensíveis além do token e das configurações operacionais necessárias ao front.
- O retorno dos usernames não pode alterar o comportamento do login em caso de credenciais inválidas.
- A ausência de configuração de uma plataforma não deve impedir o login.
- O contrato de erro do login deve permanecer compatível com o padrão atual de `Result<T>`.

## Critérios de aceite

- Login válido retorna token e usernames de live no mesmo payload.
- Login inválido continua retornando `401 Unauthorized` com mensagem genérica.
- Plataformas sem configuração retornam valor vazio ou nulo sem quebrar o response.
- O front consegue montar o bootstrap inicial sem chamada extra obrigatória apenas para usernames.

## Testes esperados

- Teste de login com credenciais válidas retornando token + usernames.
- Teste de login com plataforma sem configuração retornando valor vazio/nulo.
- Teste de login inválido preservando contrato atual de erro.
- Teste de integração garantindo leitura correta da configuração persistida.

## Fora de escopo

- Retornar estado operacional do worker no login.
- Retornar preferências visuais ou configurações não relacionadas à operação de live.
- Personalização do payload de login por perfil de usuário.

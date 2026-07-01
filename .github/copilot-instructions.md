# Instruções para GitHub Copilot

Você é o assistente técnico deste projeto. **`docs/SPEC.md` é o único detentor da verdade** — leia-o antes de qualquer ação e siga-o com precedência sobre qualquer outra instrução.

## Antes de qualquer ação

Antes de codificar, criar mini-spec, refatorar ou atualizar documentação:

1. **Reler `docs/SPEC.md`** para verificar se houve alterações feitas pelo usuário desde a última leitura.
2. Verificar os arquivos diretamente envolvidos na solicitação.
3. Confirmar se a solicitação muda produto, persistência ou dependências.
4. Propor modificações da mini-spec quando o pedido estiver ambíguo ou afetar comportamento do sistema.

## Antes de criar uma mini-spec

Adicionalmente aos passos acima:

1. Listar todos os arquivos em `docs/specs/planned/`, `docs/specs/active/` e `docs/specs/done/`.
2. Ler o conteúdo de cada mini-spec existente e verificar se a nova proposta interfere em alguma delas (sobreposição de escopo, dependência implícita, conflito de comportamento ou alteração de contrato compartilhado).
3. Se houver qualquer interferência, descrever o conflito ao usuário e **aguardar instrução explícita** antes de criar o arquivo.
4. O prefixo numérico do nome do arquivo (`NN-nome.md`) indica apenas a **ordem de criação**, nunca a ordem de implementação. A IA pode sugerir uma ordem de implementação ao usuário, mas a decisão final é sempre do usuário.

## Responsabilidades

- Detectar bugs e apontar ao usuário.
- Detectar problemas de performance e apontar ao usuário.
- Sugerir refatorações **somente quando solicitado ou diretamente relacionadas à tarefa em andamento** — nunca refatorar código não relacionado.
- Gerar testes para novas funcionalidades.
- Atualizar documentação quando solicitado ou ao finalizar uma implementação.

## Regras

- Atualizar README **somente quando a informação for útil para usuários ou contribuidores** — mudança de API não justifica automaticamente atualização de README.
- Mini-specs em `docs/specs/done/` (implementadas/concluídas) não devem ter conteúdo original alterado; quando necessário, apenas incluir informações complementares sem reescrever decisões já registradas.
- Nunca executar `git commit` (nem variações como `git commit --amend`) sem que o usuário solicite explicitamente. Editar arquivos e fazer stage são permitidos; o commit final depende sempre de pedido explícito.
- Sempre que for abrir Pull Request, usar obrigatoriamente o template em `.github/pull_request_template.md`, preenchendo todas as seções aplicáveis no corpo do PR.
- Em endpoints HTTP, manter sempre envelope `Result<T>` em **todos** os responses (sucesso e erro). A implementação e a documentação (`Produces(...)`) devem permanecer consistentes com `Result<T>`.
- Em documentação escrita em português, manter sempre consistência de ortografia, acentuação e terminologia em pt-BR (evitar variações mistas e grafias sem acento quando não intencionais).

## Ao finalizar qualquer implementação

- Descrever os arquivos alterados.
- Informar verificações executadas.
- Declarar riscos ou pendências.
- Apontar decisões que ainda precisam de confirmação humana.

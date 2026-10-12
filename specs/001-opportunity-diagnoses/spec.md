# Feature Specification: MVP de Diagnósticos de Oportunidades Tecnológicas

**Feature Branch**: `feature/001-opportunity-diagnoses`

**Created**: 2026-10-11

**Status**: Draft

**Input**: User description: "Criar o MVP de um gerador de diagnósticos de oportunidades tecnológicas para consultores registrarem informações de clientes, investigarem dores operacionais e produzirem diagnósticos editáveis com oportunidades priorizadas e plano de implementação por fases."

## Clarifications

### Session 2026-10-11

- Q: Se um setor do cliente for renomeado depois de usado em um diagnóstico, qual nome esse diagnóstico deve mostrar? → A: Preservar no diagnóstico o nome registrado quando o setor foi vinculado; alterações valem para diagnósticos futuros.

## User Scenarios & Testing *(mandatory)*

### User Story US-001 - Clientes e setores (Priority: P1)

Como consultor, quero cadastrar, localizar e atualizar clientes e seus setores, para reutilizar essas informações ao preparar diagnósticos.

**Why this priority**: Todo diagnóstico precisa estar associado a um cliente e pode avaliar setores desse cliente.

**Independent Test**: Cadastrar um cliente com dois setores, localizá-lo na lista e atualizar seus dados sem criar um diagnóstico.

**Acceptance Scenarios**:

1. **Dado** que o consultor está na lista de clientes, **quando** cadastra uma empresa com nome e segmento, **então** o cliente é salvo e aparece na lista.
2. **Dado** um formulário de cliente, **quando** nome da empresa ou segmento estiver vazio, **então** o cadastro não é aceito e o campo pendente é indicado.
3. **Dado** um cliente cadastrado, **quando** o consultor inclui um setor com nome sugerido ou personalizado, **então** o setor fica disponível para os diagnósticos daquele cliente.
4. **Dado** que existem vários clientes, **quando** o consultor pesquisa por empresa, segmento ou nome do contato, **então** a lista mostra os clientes correspondentes.
5. **Dado** um cadastro de cliente, **quando** o consultor informa nome da empresa e segmento e deixa contato, e-mail, telefone ou observações vazios, **então** o cliente pode ser salvo sem esses dados opcionais.
6. **Dado** um cliente, **quando** o consultor escolhe um setor sugerido ou informa nome, responsável e quantidade de pessoas, **então** o setor é salvo; somente seu nome é obrigatório.
7. **Dado** um cliente ou setor cadastrado, **quando** o consultor edita seus dados, **então** as alterações ficam disponíveis para os próximos diagnósticos; diagnósticos existentes preservam o nome do setor registrado quando ele foi vinculado; a aplicação não oferece exclusão de clientes.

---

### User Story US-002 - Preparar um diagnóstico (Priority: P1)

Como consultor, quero criar um diagnóstico para um cliente, selecionar objetivos e setores, para delimitar a investigação antes de registrar as respostas.

**Why this priority**: O diagnóstico organiza as informações e define quais áreas e resultados serão avaliados.

**Independent Test**: Com um cliente e setores previamente cadastrados, criar um diagnóstico, escolher mais de um objetivo, definir o principal e reabrir o rascunho.

**Acceptance Scenarios**:

1. **Dado** um cliente cadastrado, **quando** o consultor inicia um diagnóstico com título e objetivos, **então** o rascunho é associado a esse cliente e pode ser editado depois.
2. **Dado** um diagnóstico, **quando** o consultor seleciona vários objetivos e um objetivo principal entre eles, **então** ambos ficam registrados.
3. **Dado** que o objetivo principal está selecionado, **quando** o consultor tenta removê-lo dos objetivos do diagnóstico, **então** precisa escolher outro objetivo principal antes de salvar.
4. **Dado** um cliente com setores cadastrados, **quando** o consultor seleciona setores para avaliação, **então** somente setores daquele cliente podem ser associados ao diagnóstico.
5. **Dado** um cliente, **quando** o consultor salva mais de um diagnóstico, **então** todos continuam acessíveis a partir do cliente.
6. **Dado** o formulário de um diagnóstico, **quando** o consultor inicia um rascunho, **então** a data é preenchida com a data atual e pode ser ajustada antes de salvar.
7. **Dado** os objetivos disponíveis, **quando** o consultor escolhe um ou mais objetivos e define o principal, **então** pode selecionar reduzir custos, melhorar produtividade, diminuir erros e retrabalho, melhorar atendimento, aumentar vendas, integrar informações ou outro; ao escolher outro, informa sua descrição.
8. **Dado** um diagnóstico em Rascunho, **quando** o consultor muda seu status para Em andamento, **então** o novo status é salvo; as opções de status são Rascunho, Em andamento e Concluído.
9. **Dado** um diagnóstico com setores avaliados, **quando** o consultor tenta associá-lo a outro cliente, **então** a alteração é bloqueada e o motivo é informado para preservar a compatibilidade entre o cliente e os setores vinculados.

---

### User Story US-003 - Registrar entrevistas por setor (Priority: P1)

Como consultor, quero registrar respostas separadas para cada setor avaliado, para preservar o contexto de cada entrevista e continuar o preenchimento em outro momento.

**Why this priority**: As respostas documentam processos, ferramentas, dificuldades e resultados desejados que fundamentam o diagnóstico.

**Independent Test**: Em um diagnóstico com dois setores, salvar respostas parciais diferentes para cada setor, sair e retornar para confirmar os dados e o progresso.

**Acceptance Scenarios**:

1. **Dado** um setor incluído no diagnóstico, **quando** o consultor abre o questionário, **então** encontra as dez perguntas fixas definidas nos requisitos.
2. **Dado** que as respostas são opcionais, **quando** o consultor preenche algumas perguntas e aciona salvar, **então** somente as respostas informadas são persistidas e o progresso mostra a quantidade respondida de dez.
3. **Dado** um questionário parcialmente salvo, **quando** o consultor retorna ao setor, **então** as respostas anteriores aparecem e podem ser continuadas.
4. **Dado** dois setores avaliados, **quando** o consultor salva respostas para ambos, **então** as respostas de cada setor permanecem separadas.
5. **Dado** um setor do diagnóstico com respostas ou gargalos vinculados, **quando** o consultor tenta removê-lo da avaliação, **então** a remoção é impedida e o motivo é informado.

---

### User Story US-004 - Registrar gargalos (Priority: P1)

Como consultor, quero registrar gargalos vinculados a setores avaliados, para documentar problemas operacionais e seus impactos com evidências distintas de suas causas prováveis.

**Why this priority**: Gargalos conectam as entrevistas a problemas concretos que podem ser tratados por oportunidades.

**Independent Test**: Em um diagnóstico existente, cadastrar gargalos em setores avaliados e confirmar que campos obrigatórios, categorias e impacto são validados.

**Acceptance Scenarios**:

1. **Dado** um diagnóstico com um setor avaliado, **quando** o consultor informa título, setor, categoria e impacto válido, **então** o gargalo é salvo no diagnóstico.
2. **Dado** um gargalo, **quando** o consultor registra evidência e causa provável, **então** esses valores aparecem em campos distintos.
3. **Dado** que horas mensais estimadas foram informadas, **quando** o valor é negativo, **então** o sistema rejeita o valor e explica a validação.
4. **Dado** um gargalo salvo, **quando** o consultor solicita sua exclusão, **então** a aplicação pede confirmação antes de excluir.
5. **Dado** que um gargalo está associado a oportunidades, **quando** o consultor tenta excluí-lo, **então** a aplicação impede a exclusão até que as oportunidades sejam reassociadas ou removidas.
6. **Dado** o formulário de gargalo, **quando** o consultor seleciona uma categoria e impacto entre 1 e 5, **então** o gargalo aceita as categorias previstas nos requisitos e valores dentro desses limites.

---

### User Story US-005 - Cadastrar e priorizar oportunidades (Priority: P1)

Como consultor, quero relacionar oportunidades a um ou mais gargalos e comparar sua prioridade, para organizar recomendações com critérios visíveis sem automatizar minha decisão.

**Why this priority**: Oportunidades priorizadas transformam o diagnóstico em possibilidades concretas de melhoria.

**Independent Test**: Cadastrar duas oportunidades com gargalos, comparar suas pontuações e verificar a ordenação e os critérios de desempate.

**Acceptance Scenarios**:

1. **Dado** um diagnóstico com gargalos, **quando** o consultor cadastra uma oportunidade com os campos obrigatórios e ao menos um gargalo do mesmo diagnóstico, **então** a oportunidade é salva e vinculada aos gargalos escolhidos.
2. **Dado** uma oportunidade do tipo aplicação de IA, **quando** o consultor informa dados necessários e necessidade de revisão humana, **então** esses dados ficam registrados como informações opcionais da oportunidade e nenhuma recomendação é gerada automaticamente.
3. **Dado** oportunidades cadastradas, **quando** o consultor consulta a lista, **então** cada pontuação usa a fórmula definida nos requisitos e a interface explica essa fórmula.
4. **Dado** oportunidades com pontuações iguais, **quando** a lista é exibida, **então** são ordenadas por maior impacto, menor esforço e título em ordem crescente.
5. **Dado** uma oportunidade, **quando** o consultor solicita sua exclusão, **então** a aplicação pede confirmação antes de excluir.
6. **Dado** uma oportunidade com impacto 4, urgência 3 e esforço 2, **quando** sua prioridade é calculada, **então** a pontuação exibida é 9, pela fórmula `(4 × 2) + 3 − 2`.

---

### User Story US-006 - Organizar o plano por fases (Priority: P1)

Como consultor, quero organizar oportunidades em fases editáveis, para apresentar uma sequência de implementação adequada ao cliente.

**Why this priority**: As fases conectam as oportunidades priorizadas a um plano compreensível de execução.

**Independent Test**: Criar ou editar fases, alterar sua ordem, associar oportunidades e deixar uma oportunidade sem fase enquanto o diagnóstico está em elaboração.

**Acceptance Scenarios**:

1. **Dado** um diagnóstico novo, **quando** o consultor abre o plano por fases, **então** encontra sugestões de melhorias rápidas, MVP e evolução.
2. **Dado** um plano, **quando** o consultor cria, edita ou reordena fases, **então** nome, descrição e ordem ficam refletidos no plano.
3. **Dado** oportunidades cadastradas, **quando** o consultor associa algumas delas a fases, **então** o relatório apresenta cada uma na fase escolhida.
4. **Dado** uma oportunidade sem fase associada, **quando** o plano é exibido, **então** ela permanece válida e visível como não alocada, mesmo após a conclusão do diagnóstico.
5. **Dado** um plano com fases e oportunidades, **quando** o relatório é exibido, **então** as fases seguem a ordem definida pelo consultor e as oportunidades de cada fase seguem a ordem de prioridade calculada.

---

### User Story US-007 - Revisar, concluir e apresentar o diagnóstico (Priority: P1)

Como consultor, quero revisar, concluir ou reabrir o diagnóstico e apresentar um relatório editável, para validar o trabalho e compartilhar uma visão clara com o cliente.

**Why this priority**: Revisão e apresentação completam o fluxo de valor do MVP e deixam o consultor no controle das recomendações finais.

**Independent Test**: Com dados salvos de um diagnóstico, revisar o relatório, editar o resumo executivo, concluir, reabrir e imprimir uma versão legível.

**Acceptance Scenarios**:

1. **Dado** um diagnóstico com dados salvos, **quando** o consultor abre o relatório, **então** encontra identificação do cliente, objetivos, resumo executivo, setores, respostas, gargalos, oportunidades priorizadas e plano por fases.
2. **Dado** campos sem informação, **quando** o relatório é exibido, **então** esses campos aparecem como “Não informado” e nenhum conteúdo é inventado.
3. **Dado** um diagnóstico não concluído, **quando** o consultor edita o resumo executivo, recomendações gerais ou próximos passos e salva, **então** os textos editados aparecem na próxima visualização.
4. **Dado** um diagnóstico sem gargalos ou sem oportunidades, **quando** o consultor tenta concluí-lo, **então** a conclusão é impedida e os itens pendentes são indicados.
5. **Dado** um diagnóstico com objetivo principal, ao menos um gargalo e ao menos uma oportunidade, **quando** o consultor o marca como concluído, **então** seu status muda para Concluído.
6. **Dado** um diagnóstico concluído, **quando** o consultor solicita reabertura para edição, **então** o status volta para Em andamento e os dados existentes são preservados.
7. **Dado** um diagnóstico concluído, **quando** o consultor tenta editar seus dados antes de reabri-lo, **então** a edição é bloqueada e a opção de reabertura é apresentada.
8. **Dado** o relatório aberto, **quando** o consultor imprime ou salva em PDF pelo navegador, **então** o conteúdo permanece legível e navegação, botões e controles não aparecem na impressão.
9. **Dado** o consultor usando celular ou desktop, **quando** abre e navega pela aplicação sem entrar em uma tela de login, **então** o nome “Diagnóstico” e os textos estão em português do Brasil, os formulários permanecem utilizáveis e navegadores compatíveis oferecem a instalação da aplicação.
10. **Dado** que existem diagnósticos, **quando** o consultor volta à tela inicial, **então** vê os diagnósticos atualizados mais recentemente e a ação para criar outro; a aplicação não oferece exclusão de diagnósticos.

### Edge Cases

- A tentativa de salvar um cliente sem nome da empresa ou segmento deve indicar os campos obrigatórios.
- Um objetivo principal não pode ficar fora da lista de objetivos selecionados.
- Um objetivo “Outro” precisa de uma descrição que identifique o objetivo informado.
- Questionários sem respostas mostram progresso 0 de 10; respostas em branco não contam como respondidas.
- Falha de conexão durante um salvamento deve mostrar erro, manter o conteúdo ainda disponível no formulário para nova tentativa e não confirmar sucesso.
- Um diagnóstico não pode ser concluído sem objetivo principal, ao menos um gargalo e ao menos uma oportunidade.
- Uma oportunidade não pode se vincular a gargalos de outro diagnóstico nem ficar sem nenhum gargalo.
- Um gargalo vinculado a oportunidade não pode ser removido até que as referências sejam resolvidas.
- Ao tentar sair de um formulário com alterações não salvas, o consultor recebe uma confirmação; permanecer mantém os dados digitados e sair descarta somente as alterações não salvas. Falhas ao salvar mantêm o formulário aberto para nova tentativa.
- Um diagnóstico concluído não pode ser editado antes de ser reaberto.
- Valores de horas mensais menores que zero são rejeitados; o campo pode permanecer vazio.
- A pontuação pode ser negativa e ainda deve seguir a fórmula sem limitar o resultado a zero.
- Dados ausentes no relatório são exibidos como “Não informado”; respostas parciais são exibidas somente para os setores e perguntas correspondentes.
- A perda de conexão não pode transformar um salvamento não confirmado em sucesso.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A aplicação MUST exibir o nome provisório “Diagnóstico”, sem exigir identidade visual ou trabalho de branding.
- **FR-002**: A tela inicial MUST listar diagnósticos recentes e oferecer uma ação para iniciar um novo diagnóstico; a aplicação MUST oferecer uma lista de clientes e navegação por etapas dentro de cada diagnóstico.
- **FR-003**: O consultor MUST poder listar, pesquisar, cadastrar e editar clientes. Nome da empresa e segmento são obrigatórios; contato, e-mail, telefone e observações são opcionais.
- **FR-004**: O segmento do cliente MUST ser texto livre, sem exigir cadastro administrativo separado.
- **FR-005**: Cada cliente MUST poder ter vários diagnósticos.
- **FR-006**: O consultor MUST poder cadastrar e editar setores de um cliente. Nome é obrigatório; responsável e quantidade de pessoas são opcionais. A aplicação MUST sugerir Comercial, Financeiro, Atendimento, Operação e Administrativo, e aceitar nomes personalizados. Ao vincular um setor a um diagnóstico, a aplicação MUST preservar o nome do setor naquele diagnóstico; renomear o setor do cliente MUST afetar somente diagnósticos futuros.
- **FR-007**: O consultor MUST poder criar e editar diagnósticos com título, cliente, data, contexto, objetivos, objetivo principal e status. Um diagnóstico MUST estar associado a um cliente e iniciar como Rascunho. O cliente associado MUST poder ser alterado somente enquanto não houver setores avaliados vinculados ao diagnóstico.
- **FR-008**: Os status disponíveis MUST ser Rascunho, Em andamento e Concluído. O consultor MUST poder mover um diagnóstico entre Rascunho e Em andamento; a conclusão está sujeita aos critérios de conclusão. O objetivo principal MUST ser um dos objetivos selecionados. Os objetivos disponíveis MUST ser reduzir custos, melhorar produtividade, diminuir erros e retrabalho, melhorar atendimento, aumentar vendas, integrar informações e outro.
- **FR-009**: O consultor MUST poder selecionar um ou mais setores do cliente para avaliação. Um setor com respostas ou gargalos vinculados MUST permanecer associado ao diagnóstico.
- **FR-010**: Cada setor avaliado MUST ter um questionário fixo e independente, sem editor de perguntas ou regras condicionais, com estas perguntas:
  1. Quais atividades e processos esse setor executa?
  2. Quais sistemas, planilhas e ferramentas utiliza?
  3. O que mais consome tempo da equipe?
  4. Quais tarefas são repetitivas ou exigem copiar informações?
  5. Onde existem esperas, aprovações ou dependência de outras pessoas?
  6. Quais erros e retrabalhos acontecem com frequência?
  7. Que informações são difíceis de encontrar ou ficam espalhadas?
  8. Existem falhas na comunicação ou na transferência de informações entre setores?
  9. O que já foi tentado para melhorar esses problemas?
  10. Qual resultado indicaria que a melhoria deu certo?
- **FR-011**: As respostas do questionário MUST ser texto livre e opcionais. O consultor MUST salvá-las por ação explícita, retomar um preenchimento parcial e ver o progresso como quantidade de perguntas respondidas sobre dez para cada setor.
- **FR-012**: O consultor MUST poder cadastrar gargalos vinculados ao diagnóstico e a um setor avaliado, com título, descrição, categoria, impacto de 1 a 5, frequência, evidência ou exemplo, causa provável, horas mensais estimadas opcionais e observações. Título, setor, categoria e impacto são obrigatórios; horas estimadas não podem ser negativas. Evidência e causa provável MUST ser campos distintos.
- **FR-013**: As categorias de gargalo MUST incluir trabalho manual, repetição, retrabalho, demora, falta de integração, informações dispersas, dependência de pessoas, problema comercial e outro.
- **FR-014**: O consultor MUST poder cadastrar oportunidades vinculadas ao diagnóstico e a um ou mais gargalos do mesmo diagnóstico. Os campos são título, descrição da solução, tipo, benefício esperado, impacto de 1 a 5, esforço de 1 a 5, urgência de 1 a 5, dependências e uma fase opcional do plano. Título, tipo, benefício esperado, impacto, esforço, urgência e ao menos um gargalo são obrigatórios.
- **FR-015**: Os tipos de oportunidade MUST incluir melhoria de processo, ferramenta existente, integração, software sob medida, automação e aplicação de IA. Oportunidades de IA MUST permitir registrar dados necessários e necessidade de revisão humana em campos opcionais; a aplicação não deve recomendar IA automaticamente para todos os problemas.
- **FR-016**: A prioridade de cada oportunidade MUST ser calculada como `(impacto × 2) + urgência − esforço`. A aplicação MUST exibir a pontuação e explicar a fórmula. A lista MUST ordenar por pontuação decrescente e, em empate, por maior impacto, menor esforço e título em ordem crescente. A ordem da fase MUST NOT alterar a pontuação; oportunidades em cada fase MUST seguir os mesmos critérios de ordenação. A pontuação serve como auxílio ao consultor, não como decisão automática definitiva.
- **FR-017**: O consultor MUST poder criar, editar e ordenar fases com nome e descrição, associar cada oportunidade a no máximo uma fase e deixar oportunidades sem fase. A associação a uma fase MUST ser opcional e MUST NOT ser condição para concluir o diagnóstico. A ordem definida pelo consultor MUST determinar a sequência das fases no plano. A aplicação MUST sugerir inicialmente melhorias rápidas, MVP e evolução.
- **FR-018**: O relatório MUST refletir os dados salvos e conter identificação do cliente, objetivos, resumo executivo, setores avaliados, respostas, gargalos, oportunidades priorizadas e plano por fases. Resumo executivo, recomendações gerais e próximos passos MUST ser editáveis pelo consultor.
- **FR-019**: Informações ausentes no relatório MUST aparecer como “Não informado”. A aplicação MUST apresentar uma visualização limpa para o cliente e MUST permitir imprimir ou salvar em PDF pelo navegador; navegação, botões e controles MUST ficar ocultos na impressão.
- **FR-020**: A aplicação MUST impedir a conclusão de um diagnóstico sem objetivo principal, ao menos um gargalo e ao menos uma oportunidade. Um diagnóstico Concluído MUST ser somente leitura até ser reaberto; reabri-lo MUST alterar o status para Em andamento, preservar os dados existentes e permitir edição.
- **FR-021**: A aplicação MUST pedir confirmação antes de excluir gargalos ou oportunidades e MUST impedir a exclusão de clientes e diagnósticos neste MVP.
- **FR-022**: A aplicação MUST oferecer formulários responsivos para celular e desktop e ser instalável como PWA em navegadores compatíveis de celular, macOS e Windows.
- **FR-023**: A aplicação MUST exigir conexão com o serviço para criar ou alterar dados; sincronização offline de dados não faz parte do MVP. Dados confirmados como salvos MUST permanecer disponíveis após recarregar o navegador.
- **FR-024**: Todos os textos apresentados ao consultor MUST estar em português do Brasil. O consultor MUST poder usar o MVP sem login.
- **FR-025**: O salvamento de cada formulário MUST ocorrer somente após ação explícita do consultor. Ao tentar sair de um formulário com alterações não salvas, a aplicação MUST pedir confirmação; permanecer MUST manter os valores digitados, e confirmar a saída MUST descartar as alterações não salvas sem indicar que foram salvas. Ao fechar ou recarregar a página, a aplicação MUST solicitar confirmação pelos recursos do navegador quando disponíveis.

### Out of Scope

- Login, usuários, perfis, controle de acesso por papel e multi-tenancy.
- Publicação aberta na internet sem uma decisão posterior sobre controle de acesso.
- Integração com LLM, geração automática de diagnósticos ou recomendações, gravação de áudio, transcrição, anexos e upload de arquivos.
- Questionários dinâmicos, regras condicionais e sincronização offline.
- Cálculo financeiro de ROI, assinatura digital, proposta comercial e integrações externas.
- Gantt, gestão de tarefas, controle de sprints, exclusão de clientes ou exclusão de diagnósticos.
- Geração de PDF no servidor.

### Key Entities *(include if feature involves data)*

- **Cliente**: empresa entrevistada, com segmento, contato e observações, que pode ter vários setores e diagnósticos.
- **Setor**: área de um cliente que pode ser selecionada para avaliação.
- **Diagnóstico**: trabalho associado a um cliente, com data, contexto, objetivos, objetivo principal, status e resultado editável.
- **Setor avaliado**: vínculo entre um diagnóstico e um setor do mesmo cliente, com o nome preservado no momento do vínculo, respostas próprias e gargalos.
- **Resposta**: texto opcional dado a uma pergunta fixa de um setor avaliado.
- **Gargalo**: problema de um setor avaliado, com categoria, impacto, frequência, evidências e causa provável.
- **Oportunidade**: possibilidade de melhoria vinculada a um ou mais gargalos do mesmo diagnóstico e avaliada por impacto, esforço e urgência.
- **Fase**: etapa ordenada do plano de implementação de um diagnóstico, à qual oportunidades podem ser associadas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um consultor consegue concluir o cenário de aceite ponta a ponta: cadastrar um cliente com dois setores, criar diagnóstico com objetivo principal, salvar respostas parciais, registrar três gargalos e duas oportunidades, organizar fases, editar o resumo, concluir e reabrir o diagnóstico, e imprimir o relatório.
- **SC-002**: Para cada setor avaliado, o progresso exibido corresponde exatamente ao número de respostas não vazias entre as dez perguntas, inclusive após sair e retornar ao diagnóstico.
- **SC-003**: Para 100% das oportunidades cadastradas, a pontuação e a ordenação correspondem à fórmula e aos desempates definidos.
- **SC-004**: Após confirmação de salvamento, recarregar o navegador preserva os dados do cliente, diagnóstico, respostas, gargalos, oportunidades, fases e textos editáveis do relatório.
- **SC-005**: O relatório apresenta as oito seções obrigatórias, marca valores ausentes como “Não informado” e imprime sem navegação, botões ou controles.
- **SC-006**: O consultor consegue navegar pelos formulários e revisar o relatório em celular e desktop; em navegadores compatíveis, consegue adicionar a aplicação ao dispositivo pela opção de instalação do navegador.

## Assumptions

- O uso inicial é feito por um único consultor em ambiente local ou restrito; publicação aberta depende de uma decisão posterior sobre controle de acesso.
- Nome e cliente são obrigatórios ao criar um diagnóstico; a data inicial é a data atual e pode ser ajustada; contexto é opcional. Um novo diagnóstico inicia como Rascunho.
- Quando “outro” for selecionado como objetivo, o consultor informa sua descrição em texto livre.
- A pesquisa de clientes considera nome da empresa, segmento e nome do contato; resultados recentes são ordenados pela última atualização.
- Uma pergunta conta como respondida quando contém texto não vazio após remover espaços nas extremidades.
- A frequência de um gargalo é registrada como texto livre, pois não foi definido um conjunto fechado de opções.
- Os campos de gargalo e oportunidade não listados como obrigatórios podem ficar vazios.
- Reabrir um diagnóstico concluído muda seu status para Em andamento.
- O cliente associado pode ser alterado enquanto nenhum setor estiver vinculado à avaliação; após a vinculação de um setor, a relação com o cliente é imutável para preservar a integridade dos dados.
- Para preservar vínculos válidos, um gargalo associado a oportunidades não pode ser excluído até que elas sejam reassociadas ou excluídas.
- Diagnósticos concluídos ficam somente leitura até serem reabertos; não é necessário associar todas as oportunidades a uma fase para concluir o diagnóstico.
- A prioridade de oportunidades independe da fase; fases seguem a ordem definida pelo consultor e oportunidades dentro de cada fase seguem a ordenação pela fórmula de prioridade.
- Alterações não salvas não são persistidas automaticamente; a aplicação confirma antes da saída do formulário e mantém os valores caso o consultor escolha permanecer.

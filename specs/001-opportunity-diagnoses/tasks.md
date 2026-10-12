---
description: "Dependency-ordered implementation tasks for the opportunity-diagnosis MVP"
feature_id: "001"
---

# Tasks: MVP de Diagnósticos de Oportunidades Tecnológicas

**Input**: `specs/001-opportunity-diagnoses/` (`spec.md`, `plan.md`, `architecture.md`, `data-model.md`, `contracts/`, `quickstart.md`)

**Project**: [Product Development](https://github.com/users/ggbarcelos/projects/3) — repository `ggbarcelos/prisma`.

**ID synchronization**: Local task IDs are repository-wide and sequential. GitHub task titles and bodies use `001-Tnnn`. GitHub issues are children of their User Story; shared setup and polish tasks are children of the Feature. Project status maps `Todo` to backlog, `In Progress` to active work, and `Done` to verified completion.

**Tests**: Included for the business rules and persisted API workflow required by the constitution and approved plan. Tests target PostgreSQL and observable behavior, not accessors.

**Checklist format**: `- [ ] Tnnn [USn] description with path`. A `[P]` marker is omitted unless independence and disjoint write paths are established.

## Sprint 01 — 2026-10-19 — 2026-11-01

**Sprint Goal**: Permitir registrar, localizar e atualizar clientes e setores antes de preparar diagnósticos.

**Escopo**: US-001 e seus bloqueadores de setup. Estimativa inicial de US-001: 5 Story Points (sugestão); capacidade do time: não configurada; sem histórico de throughput, este plano é provisório e não presume capacidade.

**Critério de conclusão**: Critérios US-001 passam contra PostgreSQL dedicado, cliente e setores podem ser recarregados da API e a documentação local permite configurar e executar a solução.

### Phase 1 — Setup

- [x] T001 Fixar SDK .NET 10 estável e versões alinhadas dos pacotes EF Core/Npgsql e `dotnet-ef` em `global.json`, `Directory.Packages.props` e `.config/dotnet-tools.json`
- [x] T002 Criar solução com API, Blazor WebAssembly PWA, contratos e testes em `Prisma.sln`, `src/Prisma.Api/Prisma.Api.csproj`, `src/Prisma.Client/Prisma.Client.csproj`, `src/Prisma.Contracts/Prisma.Contracts.csproj` e `tests/Prisma.Api.Tests/Prisma.Api.Tests.csproj` (depende de T001)
- [x] T003 Configurar bootstrap da Minimal API, Problem Details, OpenAPI local, PostgreSQL por configuração externa e CORS de origem explícita em `src/Prisma.Api/Program.cs` e `src/Prisma.Api/appsettings.json` (depende de T002)
- [x] T004 Configurar Bootstrap, rotas iniciais e PWA instalável sem cache de API ou escrita offline em `src/Prisma.Client/Program.cs`, `src/Prisma.Client/wwwroot/manifest.webmanifest` e `src/Prisma.Client/wwwroot/service-worker.js` (depende de T002)
- [x] T005 Configurar `AppDbContext`, fixture de testes com banco `PrismaTest` separado, `.gitignore` e README inicial com segredos fora do código em `src/Prisma.Api/Data/AppDbContext.cs`, `tests/Prisma.Api.Tests/Infrastructure/PostgresWebApplicationFactory.cs`, `.gitignore` e `README.md` (depende de T003)

### Phase 2 — Foundational

**Purpose**: Finalizar a infraestrutura mínima necessária para validar a primeira fatia vertical.

### Phase 3 — US-001 Clientes e setores (P1) 🎯 MVP

**Goal**: Cadastrar, pesquisar e atualizar clientes e seus setores sem criar diagnósticos.

**Independent test**: Cadastrar um cliente com dois setores, localizar por empresa/segmento/contato e editar os dados sem diagnóstico.

- [x] T006 [US1] Escrever testes PostgreSQL para cadastro, validação, pesquisa, edição e ausência de exclusão de cliente/setor em `tests/Prisma.Api.Tests/Features/Clients/ClientTests.cs` (depende de T005)
- [x] T007 [US1] Implementar entidades Client/ClientSector, regras de campos, configurações e migration inicial em `src/Prisma.Api/Features/Clients/Client.cs`, `ClientSector.cs`, `ClientEntityConfiguration.cs` e `src/Prisma.Api/Data/Migrations/` (depende de T006)
- [x] T008 [US1] Implementar DTOs e endpoints Minimal API para listar/pesquisar/criar/editar clientes e listar/criar/editar setores em `src/Prisma.Contracts/Clients/ClientContracts.cs`, `src/Prisma.Api/Features/Clients/ClientEndpoints.cs` e `ClientSectorEndpoints.cs` (depende de T007)
- [x] T009 [US1] Implementar telas e formulários responsivos de clientes e setores com validação, estados de carregamento/erro/vazio/salvo e sugestões de setor em `src/Prisma.Client/Features/Clients/ClientsPage.razor`, `ClientForm.razor` e `ClientSectorForm.razor` (depende de T008)


## Product Backlog (sem sprint)

The first sprint is provisional because the Project has no configured capacity or throughput history. The next Monday is 2026-10-19. Only the smallest independent P1 story, US-001, and its setup blockers are committed to Sprint 01; the remaining approved feature scope stays in the Product Backlog. Implementation authorization covers the complete MVP, but no capacity or future sprint assignment is inferred.

### US-002 — Preparar um diagnóstico (P1)

**Goal**: Criar diagnósticos editáveis vinculados a um cliente, com objetivos, objetivo principal, setores compatíveis e estados iniciais.

**Independent test**: Criar dois diagnósticos para um cliente, selecionar objetivos e setores, reabrir um rascunho e rejeitar setores de outro cliente.

- [x] T010 [US2] Escrever testes PostgreSQL para objetivos, cliente, setor de outro cliente, alteração de cliente após avaliação e status inicial em `tests/Prisma.Api.Tests/Features/Diagnoses/DiagnosisTests.cs` (depende de T009)
- [x] T011 [US2] Implementar entidades, chaves compostas, objetivo principal único e migration de diagnóstico e setores avaliados em `src/Prisma.Api/Features/Diagnoses/` e `src/Prisma.Api/Data/Migrations/` (depende de T010)
- [x] T012 [US2] Implementar contratos e endpoints para criar, listar, consultar e editar diagnóstico, objetivos e setores avaliados em `src/Prisma.Contracts/Diagnoses/DiagnosisContracts.cs` e `src/Prisma.Api/Features/Diagnoses/DiagnosisEndpoints.cs` (depende de T011)
- [x] T013 [US2] Implementar início, lista recente, navegação, edição de objetivos/setores e status Rascunho/Em andamento em `src/Prisma.Client/Features/Diagnoses/DiagnosisEditor.razor`, `DiagnosisDetail.razor` e `src/Prisma.Client/Features/Home/HomePage.razor` (depende de T012)

### US-003 — Registrar entrevistas por setor (P1)

**Goal**: Salvar respostas opcionais e independentes para as dez perguntas fixas, com progresso persistido por setor avaliado.

**Independent test**: Salvar respostas parciais em dois setores, sair e retornar, e conferir conteúdo e contagem de respostas não vazias.

- [x] T014 [US3] Escrever testes PostgreSQL para respostas parciais, substituição explícita, isolamento por setor e progresso de 0 a 10 em `tests/Prisma.Api.Tests/Features/SectorAssessments/SectorAssessmentTests.cs` (depende de T013)
- [x] T015 [US3] Adicionar as dez respostas opcionais e migration de avaliação setorial em `src/Prisma.Api/Features/SectorAssessments/SectorAssessment.cs` e `src/Prisma.Api/Data/Migrations/` (depende de T014)
- [x] T016 [US3] Implementar contrato e endpoints de leitura/salvamento explícito das respostas em `src/Prisma.Contracts/SectorAssessments/SectorAssessmentContracts.cs` e `src/Prisma.Api/Features/SectorAssessments/SectorAssessmentEndpoints.cs` (depende de T015)
- [x] T017 [US3] Implementar questionário fixo em português, progresso, estados de API e proteção de navegação com alterações não salvas em `src/Prisma.Client/Features/SectorAssessments/SectorAssessmentForm.razor` e `src/Prisma.Client/Shared/UnsavedChangesGuard.razor` (depende de T016)

### US-004 — Registrar gargalos (P1)

**Goal**: Registrar gargalos com evidência e causa separadas, validações e vínculos com uma avaliação do mesmo diagnóstico.

**Independent test**: Criar gargalos em setores avaliados, rejeitar horas negativas e impedir remoção de avaliação/gargalo com dependências.

- [x] T018 [US4] Escrever testes PostgreSQL para campos obrigatórios, categorias, impacto, horas não negativas e bloqueio de remoção relacionada em `tests/Prisma.Api.Tests/Features/Bottlenecks/BottleneckTests.cs` (depende de T017)
- [x] T019 [US4] Implementar entidade, integridade de vínculo com avaliação do mesmo diagnóstico e migration de gargalo em `src/Prisma.Api/Features/Bottlenecks/Bottleneck.cs` e `src/Prisma.Api/Data/Migrations/` (depende de T018)
- [x] T020 [US4] Implementar contratos e endpoints de listar, criar, editar e excluir gargalos com validação de propriedade em `src/Prisma.Contracts/Bottlenecks/BottleneckContracts.cs` e `src/Prisma.Api/Features/Bottlenecks/BottleneckEndpoints.cs` (depende de T019)
- [x] T021 [US4] Implementar lista e formulário responsivos de gargalo, validações, confirmação de exclusão e mensagem de bloqueio em `src/Prisma.Client/Features/Bottlenecks/BottleneckList.razor` e `BottleneckForm.razor` (depende de T020)

### US-005 — Cadastrar e priorizar oportunidades (P1)

**Goal**: Associar cada oportunidade a gargalos do mesmo diagnóstico, validar entradas e exibir a prioridade calculada e seus desempates.

**Independent test**: Criar oportunidades, verificar fórmula, pontuação negativa, desempates e rejeição de gargalos de outro diagnóstico ou de lista vazia.

- [x] T022 [US5] Escrever testes PostgreSQL para fórmula, ordenação, limites, vínculo não vazio e integridade entre diagnósticos em `tests/Prisma.Api.Tests/Features/Opportunities/OpportunityTests.cs` (depende de T021)
- [x] T023 [US5] Implementar entidades Opportunity/OpportunityBottleneck, chaves compostas e migration de oportunidades em `src/Prisma.Api/Features/Opportunities/` e `src/Prisma.Api/Data/Migrations/` (depende de T022)
- [x] T024 [US5] Implementar contratos e endpoints CRUD com score derivado, ordenação estável e validação de vínculos em `src/Prisma.Contracts/Opportunities/OpportunityContracts.cs` e `src/Prisma.Api/Features/Opportunities/OpportunityEndpoints.cs` (depende de T023)
- [x] T025 [US5] Implementar formulário e lista priorizada com explicação da fórmula, tipos, campos opcionais de IA e exclusão confirmada em `src/Prisma.Client/Features/Opportunities/OpportunityList.razor` e `OpportunityForm.razor` (depende de T024)

### US-006 — Organizar o plano por fases (P1)

**Goal**: Criar, editar e ordenar fases, distribuir oportunidades sem alterar sua prioridade e permitir oportunidades não alocadas.

**Independent test**: Conferir as três fases iniciais, reordenar fases, associar oportunidade da mesma análise e manter outra sem fase.

- [x] T026 [US6] Escrever testes PostgreSQL para fases iniciais, edição, reordenação, vínculo opcional e rejeição de fase de outro diagnóstico em `tests/Prisma.Api.Tests/Features/Phases/PhaseTests.cs` (depende de T025)
- [x] T027 [US6] Implementar entidade, ordem única por diagnóstico, chaves compostas, três fases iniciais e migration em `src/Prisma.Api/Features/Phases/Phase.cs`, `src/Prisma.Api/Features/Diagnoses/DiagnosisEndpoints.cs` e `src/Prisma.Api/Data/Migrations/` (depende de T026)
- [x] T028 [US6] Implementar contratos e endpoints de edição/reordenação e associação opcional de oportunidades à fase do mesmo diagnóstico em `src/Prisma.Contracts/Phases/PhaseContracts.cs`, `src/Prisma.Api/Features/Phases/PhaseEndpoints.cs` e `src/Prisma.Api/Features/Opportunities/OpportunityEndpoints.cs` (depende de T027)
- [x] T029 [US6] Implementar editor de fases, controles de reordenação, seleção de fase e lista de oportunidades não alocadas em `src/Prisma.Client/Features/Phases/PhasePlan.razor` (depende de T028)

### US-007 — Revisar, concluir e apresentar o diagnóstico (P1)

**Goal**: Exibir relatório fiel aos dados salvos, editar textos, imprimir pelo navegador e concluir/reabrir respeitando regras de leitura.

**Independent test**: Revisar relatório com campos ausentes, editar e salvar, concluir com pré-requisitos, bloquear edição, reabrir preservando dados e imprimir sem controles.

- [x] T030 [US7] Escrever testes PostgreSQL para pré-requisitos de conclusão, transições, bloqueio de mutações concluídas e preservação após reabertura em `tests/Prisma.Api.Tests/Features/Diagnoses/DiagnosisStatusTests.cs` (depende de T029)
- [x] T031 [US7] Implementar projeção do relatório e leitura/salvamento explícito de resumo, recomendações e próximos passos em `src/Prisma.Contracts/Reports/DiagnosisReportContracts.cs` e `src/Prisma.Api/Features/Reports/ReportEndpoints.cs` (depende de T030)
- [x] T032 [US7] Implementar transições de conclusão/reabertura e rejeitar todas as mutações de conteúdo quando concluído em `src/Prisma.Api/Features/Diagnoses/DiagnosisEndpoints.cs` e nos endpoints de escrita das demais fatias (depende de T031)
- [x] T033 [US7] Implementar relatório em português com dados ausentes como “Não informado”, edição explícita, confirmação de salvamento, impressão e CSS sem controles em `src/Prisma.Client/Features/Reports/DiagnosisReport.razor` e `src/Prisma.Client/wwwroot/css/app.css` (depende de T032)

## Dependências e ordem de execução

- Setup T001–T005 precede qualquer história porque estabelece solução, persistência e testes.
- Sprint 01 entrega US-001 (T006–T009), independente de diagnósticos.
- As histórias seguintes preservam a ordem de dependências: US-002 → US-003 → US-004 → US-005 → US-006 → US-007.
- Dentro de cada história, escrever testes relevantes antes da implementação, aplicar migration revisada, implementar API e então interface.
- T035–T038 são validações finais da jornada, operação e documentação, não autorizam expansão de escopo.

## Polish e validação transversal (Product Backlog)

- [ ] T034 [US7] Verificar instalação PWA em navegador compatível, formulários responsivos, falha sem API sem confirmação de sucesso e impressão sem controles, atualizando evidências em `specs/001-opportunity-diagnoses/quickstart.md` (depende de T033)
- [x] T035 [US7] Completar teste integrado persistido do fluxo principal de cliente até impressão/projeção do relatório em `tests/Prisma.Api.Tests/Features/Diagnoses/DiagnosisWorkflowTests.cs` (depende de T033)
- [x] T036 Atualizar instruções finais de configuração, execução, User Secrets e migrations em `README.md` e `specs/001-opportunity-diagnoses/quickstart.md` (depende de T035)
- [x] T037 Validar solução, testes PostgreSQL e migrations conforme `specs/001-opportunity-diagnoses/quickstart.md`, corrigindo somente defeitos necessários ao aceite
- [x] T038 Revisar diff, critérios da especificação, arquitetura por fatia e segurança de logs/segredos em `specs/001-opportunity-diagnoses/architecture.md`, `README.md` e todo o código alterado

## Estratégia de implementação

1. Completar setup e US-001 como primeiro incremento demonstrável.
2. Continuar as histórias aprovadas na ordem de dependências para completar o fluxo integral do MVP.
3. Em cada história, validar comportamento isoladamente antes de avançar.
4. Executar a jornada PostgreSQL e as verificações do `quickstart.md` após a última história.
5. Não criar autenticação, integrações de IA, sincronização offline, upload, PDF no servidor ou outras capacidades fora do escopo.

## Sincronização com o Project

Project V2 [Product Development](https://github.com/users/ggbarcelos/projects/3), autorizado e associado ao repositório `ggbarcelos/prisma`. O Project contém 47 itens (1 Epic, 1 Feature, 7 User Stories e 38 Tasks); seu resumo de status é mantido nas linhas por item abaixo e sincronizado com o GitHub. A Sprint 01 está atribuída à US-001 e às tarefas T001–T009. IDs de tarefa remotos usam o prefixo `001-Tnnn`.

| Item local | GitHub Issue | Pai | Sprint | Status |
|---|---|---|---|---|
| Épico | [#1](https://github.com/ggbarcelos/prisma/issues/1) | — | — | Todo |
| Feature 001 | [#2](https://github.com/ggbarcelos/prisma/issues/2) | Epic #1 | — | In Progress |
| US-001 — Clientes e setores | [#3](https://github.com/ggbarcelos/prisma/issues/3) | Feature #2 | Sprint 01 — 2026-10-19 | In Progress |
| US-002 — Preparar um diagnóstico | [#4](https://github.com/ggbarcelos/prisma/issues/4) | Feature #2 | Product Backlog | In Progress |
| US-003 — Registrar entrevistas por setor | [#5](https://github.com/ggbarcelos/prisma/issues/5) | Feature #2 | Product Backlog | In Progress |
| US-004 — Registrar gargalos | [#6](https://github.com/ggbarcelos/prisma/issues/6) | Feature #2 | Product Backlog | In Progress |
| US-005 — Cadastrar e priorizar oportunidades | [#7](https://github.com/ggbarcelos/prisma/issues/7) | Feature #2 | Product Backlog | In Progress |
| US-006 — Organizar o plano por fases | [#8](https://github.com/ggbarcelos/prisma/issues/8) | Feature #2 | Product Backlog | Done |
| US-007 — Revisar, concluir e apresentar o diagnóstico | [#9](https://github.com/ggbarcelos/prisma/issues/9) | Feature #2 | Product Backlog | In Progress |
| 001-T001 — Fixar SDK .NET 10 estável e versões alinhadas dos pacotes EF Core/Npgsql e `dotnet-ef` em `global.json`, `Directory.Packages.props` e `.config/dotnet-tools.json` | [#34](https://github.com/ggbarcelos/prisma/issues/34) | Feature #2 | Sprint 01 — 2026-10-19 | Done |
| 001-T002 — Criar solução com API, Blazor WebAssembly PWA, contratos e testes em `Prisma.sln`, `src/Prisma.Api/Prisma.Api.csproj`, `src/Prisma.Client/Prisma.Client.csproj`, `src/Prisma.Contracts/Prisma.Contracts.csproj` e `tests/Prisma.Api.Tests/Prisma.Api.Tests.csproj` | [#35](https://github.com/ggbarcelos/prisma/issues/35) | Feature #2 | Sprint 01 — 2026-10-19 | Done |
| 001-T003 — Configurar bootstrap da Minimal API, Problem Details, OpenAPI local, PostgreSQL por configuração externa e CORS de origem explícita em `src/Prisma.Api/Program.cs` e `src/Prisma.Api/appsettings.json` | [#36](https://github.com/ggbarcelos/prisma/issues/36) | Feature #2 | Sprint 01 — 2026-10-19 | Done |
| 001-T004 — Configurar Bootstrap, rotas iniciais e PWA instalável sem cache de API ou escrita offline em `src/Prisma.Client/Program.cs`, `src/Prisma.Client/wwwroot/manifest.webmanifest` e `src/Prisma.Client/wwwroot/service-worker.js` | [#37](https://github.com/ggbarcelos/prisma/issues/37) | Feature #2 | Sprint 01 — 2026-10-19 | Done |
| 001-T005 — Configurar `AppDbContext`, fixture de testes com banco `PrismaTest` separado, `.gitignore` e README inicial com segredos fora do código em `src/Prisma.Api/Data/AppDbContext.cs`, `tests/Prisma.Api.Tests/Infrastructure/PostgresWebApplicationFactory.cs`, `.gitignore` e `README.md` | [#38](https://github.com/ggbarcelos/prisma/issues/38) | Feature #2 | Sprint 01 — 2026-10-19 | Done |
| 001-T006 — Escrever testes PostgreSQL para cadastro, validação, pesquisa, edição e ausência de exclusão de cliente/setor em `tests/Prisma.Api.Tests/Features/Clients/ClientTests.cs` | [#39](https://github.com/ggbarcelos/prisma/issues/39) | US-001 #3 | Sprint 01 — 2026-10-19 | Done |
| 001-T007 — Implementar entidades Client/ClientSector, regras de campos, configurações e migration inicial em `src/Prisma.Api/Features/Clients/Client.cs`, `ClientSector.cs`, `ClientEntityConfiguration.cs` e `src/Prisma.Api/Data/Migrations/` | [#40](https://github.com/ggbarcelos/prisma/issues/40) | US-001 #3 | Sprint 01 — 2026-10-19 | Done |
| 001-T008 — Implementar DTOs e endpoints Minimal API para listar/pesquisar/criar/editar clientes e listar/criar/editar setores em `src/Prisma.Contracts/Clients/ClientContracts.cs`, `src/Prisma.Api/Features/Clients/ClientEndpoints.cs` e `ClientSectorEndpoints.cs` | [#41](https://github.com/ggbarcelos/prisma/issues/41) | US-001 #3 | Sprint 01 — 2026-10-19 | Done |
| 001-T009 — Implementar telas e formulários responsivos de clientes e setores com validação, estados de carregamento/erro/vazio/salvo e sugestões de setor em `src/Prisma.Client/Features/Clients/ClientsPage.razor`, `ClientForm.razor` e `ClientSectorForm.razor` | [#42](https://github.com/ggbarcelos/prisma/issues/42) | US-001 #3 | Sprint 01 — 2026-10-19 | Done |
| 001-T010 — Escrever testes PostgreSQL para objetivos, cliente, setor de outro cliente, alteração de cliente após avaliação e status inicial em `tests/Prisma.Api.Tests/Features/Diagnoses/DiagnosisTests.cs` | [#10](https://github.com/ggbarcelos/prisma/issues/10) | US-002 #4 | Product Backlog | Done |
| 001-T011 — Implementar entidades, chaves compostas, objetivo principal único e migration de diagnóstico e setores avaliados em `src/Prisma.Api/Features/Diagnoses/` e `src/Prisma.Api/Data/Migrations/` | [#11](https://github.com/ggbarcelos/prisma/issues/11) | US-002 #4 | Product Backlog | Done |
| 001-T012 — Implementar contratos e endpoints para criar, listar, consultar e editar diagnóstico, objetivos e setores avaliados em `src/Prisma.Contracts/Diagnoses/DiagnosisContracts.cs` e `src/Prisma.Api/Features/Diagnoses/DiagnosisEndpoints.cs` | [#12](https://github.com/ggbarcelos/prisma/issues/12) | US-002 #4 | Product Backlog | Done |
| 001-T013 — Implementar início, lista recente, navegação, edição de objetivos/setores e status Rascunho/Em andamento em `src/Prisma.Client/Features/Diagnoses/DiagnosisEditor.razor`, `DiagnosisDetail.razor` e `src/Prisma.Client/Features/Home/HomePage.razor` | [#13](https://github.com/ggbarcelos/prisma/issues/13) | US-002 #4 | Product Backlog | Done |
| 001-T014 — Escrever testes PostgreSQL para respostas parciais, substituição explícita, isolamento por setor e progresso de 0 a 10 em `tests/Prisma.Api.Tests/Features/SectorAssessments/SectorAssessmentTests.cs` | [#14](https://github.com/ggbarcelos/prisma/issues/14) | US-003 #5 | Product Backlog | Done |
| 001-T015 — Adicionar as dez respostas opcionais e migration de avaliação setorial em `src/Prisma.Api/Features/SectorAssessments/SectorAssessment.cs` e `src/Prisma.Api/Data/Migrations/` | [#15](https://github.com/ggbarcelos/prisma/issues/15) | US-003 #5 | Product Backlog | Done |
| 001-T016 — Implementar contrato e endpoints de leitura/salvamento explícito das respostas em `src/Prisma.Contracts/SectorAssessments/SectorAssessmentContracts.cs` e `src/Prisma.Api/Features/SectorAssessments/SectorAssessmentEndpoints.cs` | [#16](https://github.com/ggbarcelos/prisma/issues/16) | US-003 #5 | Product Backlog | Done |
| 001-T017 — Implementar questionário fixo em português, progresso, estados de API e proteção de navegação com alterações não salvas em `src/Prisma.Client/Features/SectorAssessments/SectorAssessmentForm.razor` e `src/Prisma.Client/Shared/UnsavedChangesGuard.razor` | [#17](https://github.com/ggbarcelos/prisma/issues/17) | US-003 #5 | Product Backlog | Done |
| 001-T018 — Escrever testes PostgreSQL para campos obrigatórios, categorias, impacto, horas não negativas e bloqueio de remoção relacionada em `tests/Prisma.Api.Tests/Features/Bottlenecks/BottleneckTests.cs` | [#18](https://github.com/ggbarcelos/prisma/issues/18) | US-004 #6 | Product Backlog | Done |
| 001-T019 — Implementar entidade, integridade de vínculo com avaliação do mesmo diagnóstico e migration de gargalo em `src/Prisma.Api/Features/Bottlenecks/Bottleneck.cs` e `src/Prisma.Api/Data/Migrations/` | [#19](https://github.com/ggbarcelos/prisma/issues/19) | US-004 #6 | Product Backlog | Done |
| 001-T020 — Implementar contratos e endpoints de listar, criar, editar e excluir gargalos com validação de propriedade em `src/Prisma.Contracts/Bottlenecks/BottleneckContracts.cs` e `src/Prisma.Api/Features/Bottlenecks/BottleneckEndpoints.cs` | [#20](https://github.com/ggbarcelos/prisma/issues/20) | US-004 #6 | Product Backlog | Done |
| 001-T021 — Implementar lista e formulário responsivos de gargalo, validações, confirmação de exclusão e mensagem de bloqueio em `src/Prisma.Client/Features/Bottlenecks/BottleneckList.razor` e `BottleneckForm.razor` | [#21](https://github.com/ggbarcelos/prisma/issues/21) | US-004 #6 | Product Backlog | Done |
| 001-T022 — Escrever testes PostgreSQL para fórmula, ordenação, limites, vínculo não vazio e integridade entre diagnósticos em `tests/Prisma.Api.Tests/Features/Opportunities/OpportunityTests.cs` | [#22](https://github.com/ggbarcelos/prisma/issues/22) | US-005 #7 | Product Backlog | Done |
| 001-T023 — Implementar entidades Opportunity/OpportunityBottleneck, chaves compostas e migration de oportunidades em `src/Prisma.Api/Features/Opportunities/` e `src/Prisma.Api/Data/Migrations/` | [#23](https://github.com/ggbarcelos/prisma/issues/23) | US-005 #7 | Product Backlog | Done |
| 001-T024 — Implementar contratos e endpoints CRUD com score derivado, ordenação estável e validação de vínculos em `src/Prisma.Contracts/Opportunities/OpportunityContracts.cs` e `src/Prisma.Api/Features/Opportunities/OpportunityEndpoints.cs` | [#24](https://github.com/ggbarcelos/prisma/issues/24) | US-005 #7 | Product Backlog | Done |
| 001-T025 — Implementar formulário e lista priorizada com explicação da fórmula, tipos, campos opcionais de IA e exclusão confirmada em `src/Prisma.Client/Features/Opportunities/OpportunityList.razor` e `OpportunityForm.razor` | [#25](https://github.com/ggbarcelos/prisma/issues/25) | US-005 #7 | Product Backlog | Done |
| 001-T026 — Escrever testes PostgreSQL para fases iniciais, edição, reordenação, vínculo opcional e rejeição de fase de outro diagnóstico em `tests/Prisma.Api.Tests/Features/Phases/PhaseTests.cs` | [#26](https://github.com/ggbarcelos/prisma/issues/26) | US-006 #8 | Product Backlog | Done |
| 001-T027 — Implementar entidade, ordem única por diagnóstico, chaves compostas, três fases iniciais e migration em `src/Prisma.Api/Features/Phases/Phase.cs`, `src/Prisma.Api/Features/Diagnoses/DiagnosisEndpoints.cs` e `src/Prisma.Api/Data/Migrations/` | [#27](https://github.com/ggbarcelos/prisma/issues/27) | US-006 #8 | Product Backlog | Done |
| 001-T028 — Implementar contratos e endpoints de edição/reordenação e associação opcional de oportunidades à fase do mesmo diagnóstico em `src/Prisma.Contracts/Phases/PhaseContracts.cs`, `src/Prisma.Api/Features/Phases/PhaseEndpoints.cs` e `src/Prisma.Api/Features/Opportunities/OpportunityEndpoints.cs` | [#28](https://github.com/ggbarcelos/prisma/issues/28) | US-006 #8 | Product Backlog | Done |
| 001-T029 — Implementar editor de fases, controles de reordenação, seleção de fase e lista de oportunidades não alocadas em `src/Prisma.Client/Features/Phases/PhasePlan.razor` | [#29](https://github.com/ggbarcelos/prisma/issues/29) | US-006 #8 | Product Backlog | Done |
| 001-T030 — Escrever testes PostgreSQL para pré-requisitos de conclusão, transições, bloqueio de mutações concluídas e preservação após reabertura em `tests/Prisma.Api.Tests/Features/Diagnoses/DiagnosisStatusTests.cs` | [#30](https://github.com/ggbarcelos/prisma/issues/30) | US-007 #9 | Product Backlog | Done |
| 001-T031 — Implementar projeção do relatório e leitura/salvamento explícito de resumo, recomendações e próximos passos em `src/Prisma.Contracts/Reports/DiagnosisReportContracts.cs` e `src/Prisma.Api/Features/Reports/ReportEndpoints.cs` | [#31](https://github.com/ggbarcelos/prisma/issues/31) | US-007 #9 | Product Backlog | Done |
| 001-T032 — Implementar transições de conclusão/reabertura e rejeitar todas as mutações de conteúdo quando concluído em `src/Prisma.Api/Features/Diagnoses/DiagnosisEndpoints.cs` e nos endpoints de escrita das demais fatias | [#32](https://github.com/ggbarcelos/prisma/issues/32) | US-007 #9 | Product Backlog | Done |
| 001-T033 — Implementar relatório em português com dados ausentes como “Não informado”, edição explícita, confirmação de salvamento, impressão e CSS sem controles em `src/Prisma.Client/Features/Reports/DiagnosisReport.razor` e `src/Prisma.Client/wwwroot/css/app.css` | [#33](https://github.com/ggbarcelos/prisma/issues/33) | US-007 #9 | Product Backlog | Done |
| 001-T034 — Verificar instalação PWA em navegador compatível, formulários responsivos, falha sem API sem confirmação de sucesso e impressão sem controles, atualizando evidências em `specs/001-opportunity-diagnoses/quickstart.md` | [#43](https://github.com/ggbarcelos/prisma/issues/43) | US-007 #9 | Product Backlog | In Progress |
| 001-T035 — Completar teste integrado persistido do fluxo principal de cliente até impressão/projeção do relatório em `tests/Prisma.Api.Tests/Features/Diagnoses/DiagnosisWorkflowTests.cs` | [#44](https://github.com/ggbarcelos/prisma/issues/44) | US-007 #9 | Product Backlog | Done |
| 001-T036 — Atualizar instruções finais de configuração, execução, User Secrets e migrations em `README.md` e `specs/001-opportunity-diagnoses/quickstart.md` | [#45](https://github.com/ggbarcelos/prisma/issues/45) | Feature #2 | Product Backlog | Done |
| 001-T037 — Validar solução, testes PostgreSQL e migrations conforme `specs/001-opportunity-diagnoses/quickstart.md`, corrigindo somente defeitos necessários ao aceite | [#46](https://github.com/ggbarcelos/prisma/issues/46) | Feature #2 | Product Backlog | Done |
| 001-T038 — Revisar diff, critérios da especificação, arquitetura por fatia e segurança de logs/segredos em `specs/001-opportunity-diagnoses/architecture.md`, `README.md` e todo o código alterado | [#47](https://github.com/ggbarcelos/prisma/issues/47) | Feature #2 | Product Backlog | Done |

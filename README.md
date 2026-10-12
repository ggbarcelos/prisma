# Prisma

MVP local para diagnósticos de oportunidades tecnológicas. A aplicação exige acesso à API para salvar dados.

## Executar localmente

Requer o SDK .NET 10.0.302 e PostgreSQL. Restaure as ferramentas e dependências:

```sh
dotnet tool restore
dotnet restore
```

Configure a conexão PostgreSQL da API com User Secrets e aplique as migrations:

```sh
dotnet user-secrets init --project src/Prisma.Api
dotnet user-secrets set "ConnectionStrings:Prisma" "Host=localhost;Port=5432;Database=prisma;Username=prisma_app;Password=REPLACE_LOCALLY" --project src/Prisma.Api
dotnet ef database update --project src/Prisma.Api --startup-project src/Prisma.Api
```

Execute API e cliente em terminais separados:

```sh
dotnet run --project src/Prisma.Api
dotnet run --project src/Prisma.Client
```

Para publicar o Blazor PWA servido pela API, configurar o PostgreSQL e executar os testes com um banco separado, siga o [quickstart](specs/001-opportunity-diagnoses/quickstart.md). Mantenha a aplicação local ou restrita: ela não tem autenticação.

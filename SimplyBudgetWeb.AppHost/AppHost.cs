using SimplyBudgetWeb.AppHost;
using SimplyBudgetWeb.Core;

using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

// Use the existing Container App Environment in KebooDev resource group
var envName = builder.AddParameter("keboodev-env-name", "keboodev-env");
var resourceGroup = builder.AddParameter("keboodev-rg-name", "KebooDev");
builder.AddAzureContainerAppEnvironment("keboodev-env")
    .AsExisting(envName, resourceGroup);

// Entra ID (Azure AD) App Registration used for MSAL sign-in (frontend) and API
// authorization (backend). Not configured in appsettings so that the AppHost can be
// the single source of truth shared by both the backend (AzureAd:ClientId/TenantId)
// and the frontend (ENTRA_CLIENT_ID/ENTRA_TENANT_ID) - without these, MSAL sends an
// empty client_id and Entra responds with AADSTS900144.
var entraClientId = builder.AddParameter("entra-client-id")
    .WithDescription("Client (application) ID of the Entra ID App Registration used for end-user sign-in (MSAL SPA) and API authorization.");
var entraTenantId = builder.AddParameter("entra-tenant-id")
    .WithDescription("Tenant ID of the Entra ID directory hosting the App Registration used for sign-in.");

var docsGroup = builder.AddLogicalGroup("docs");
builder.AddAspireDocs().WithParentRelationship(docsGroup);
builder.AddVuetifyDocs().WithParentRelationship(docsGroup);

IResourceBuilder<IResourceWithConnectionString> db;

if (builder.ExecutionContext.IsPublishMode)
{
    // In publish mode, use the existing Azure SQL database.
    // The connection string (pointing to keboodevdb with SimplyBudget schema) is injected
    // via environment variable by Terraform: ConnectionStrings__Database
    db = builder.AddConnectionString(ConnectionStrings.DatabaseKey);
}
else
{
    var sql = builder.AddSqlServer();
    db = sql.AddSqlDatabase();

    //DBGate is a database viewer
    var dbGate = builder.AddContainer("dbgate", "dbgate/dbgate")
        .ExcludeFromManifest()
        .ExcludeFromMcp()
        .WithExplicitStart()
        .WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("SimplyBudgetWeb-db-gate")
        .WithHttpEndpoint(targetPort: 3000)
        .WaitFor(sql)
        .WithEnvironment("CONNECTIONS", "mssql")
        .WithEnvironment("LABEL_mssql", "MS SQL")
        .WithEnvironment("SERVER_mssql", "host.docker.internal")
        .WithEnvironment("PORT_mssql", () => $"{sql.Resource.PrimaryEndpoint.Port}")
        .WithEnvironment("USER_mssql", "sa")
        .WithEnvironment("PASSWORD_mssql", sql.Resource.PasswordParameter)
        .WithEnvironment("ENGINE_mssql", "mssql@dbgate-plugin-mssql")
        .WithParentRelationship(sql)
        .WithHttpHealthCheck("/")
        ;
}

var backend = builder.AddProject<Projects.SimplyBudgetWeb>("SimplyBudgetWeb-backend")
    .WithDependency(db, ConnectionStrings.DatabaseKey)
    .WithEnvironment("AzureAd__ClientId", entraClientId)
    .WithEnvironment("AzureAd__TenantId", entraTenantId)
    .WithUITests()
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((infra, app) => app.Template.Scale.MaxReplicas = 1);

var frontendApp = builder.AddJavaScriptApp(Resources.Frontend, "../SimplyBudgetWeb.Web", "dev")
    .WithPnpm(install: true)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .WithDependency(backend)
    .WithEnvironment("REACTAPP_BACKEND_HTTP", backend.GetEndpoint("http"))
    .WithEnvironment("REACTAPP_BACKEND_HTTPS", backend.GetEndpoint("https"))
    .WithEnvironment("ENTRA_CLIENT_ID", entraClientId)
    .WithEnvironment("ENTRA_TENANT_ID", entraTenantId);

if (builder.ExecutionContext.IsPublishMode)
{
    // Enable migrations on startup for Azure deployments
    // Applying migrations on startup is not recommended for production scenarios.
    // See: https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying?tabs=dotnet-core-cli&WT.mc_id=DT-MVP-5003472
    backend.WithEnvironment("RunMigrationsOnStartup", "true");
}

builder.Build().Run();

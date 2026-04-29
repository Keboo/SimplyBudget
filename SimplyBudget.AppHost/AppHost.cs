var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("keboodev-env");

IResourceBuilder<IResourceWithConnectionString> db;

if (builder.ExecutionContext.IsPublishMode)
{
    // Reference existing keboodevdb via a connection string parameter
    db = builder.AddConnectionString("Database");
}
else
{
    var sql = builder.AddSqlServer("sql")
        .WithDataVolume();
    db = sql.AddDatabase("Database");

    // DBGate for local DB inspection
    builder.AddContainer("dbgate", "dbgate/dbgate")
        .ExcludeFromManifest()
        .WithExplicitStart()
        .WithLifetime(ContainerLifetime.Persistent)
        .WithContainerName("simplybudget-dbgate")
        .WithHttpEndpoint(targetPort: 3000)
        .WaitFor(sql)
        .WithEnvironment("CONNECTIONS", "mssql")
        .WithEnvironment("LABEL_mssql", "SimplyBudget Dev")
        .WithEnvironment("SERVER_mssql", "host.docker.internal")
        .WithEnvironment("PORT_mssql", () => $"{sql.Resource.PrimaryEndpoint.Port}")
        .WithEnvironment("USER_mssql", "sa")
        .WithEnvironment("PASSWORD_mssql", sql.Resource.PasswordParameter)
        .WithEnvironment("ENGINE_mssql", "mssql@dbgate-plugin-mssql")
        .WithParentRelationship(sql);
}

var backend = builder.AddProject<Projects.SimplyBudget_Api>("simplybudget-api")
    .WaitFor(db)
    .WithReference(db, "Database")
    .WithExternalHttpEndpoints()
    .PublishAsAzureContainerApp((_, app) => app.Template.Scale.MaxReplicas = 1);

builder.AddNpmApp("simplybudget-frontend", "../SimplyBudget.Web", "dev")
    .WithNpmPackages()
    .WithHttpEndpoint(env: "PORT")
    .ExcludeFromManifest()
    .WaitFor(backend)
    .WithReference(backend)
    .WithEnvironment("SIMPLYBUDGET_API_HTTP", backend.GetEndpoint("http"))
    .WithEnvironment("SIMPLYBUDGET_API_HTTPS", backend.GetEndpoint("https"));

if (builder.ExecutionContext.IsPublishMode)
{
    backend.WithEnvironment("RunMigrationsOnStartup", "true");
}

builder.Build().Run();

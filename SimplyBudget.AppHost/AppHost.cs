var builder = DistributedApplication.CreateBuilder(args);

var sql = builder
    .AddSqlServer("sqlserver")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithContainerName("simplybudget-sqlserver")
    .WithDataVolume("simplybudget");
var db = sql.AddDatabase("simplybudget");


var server = builder.AddProject<Projects.SimplyBudget_Server>("server")
    .WithReference(db);

var desktop = builder.AddProject<Projects.SimplyBudgetDesktop>("desktop")
    .WithReference(server);

builder.Build().Run();

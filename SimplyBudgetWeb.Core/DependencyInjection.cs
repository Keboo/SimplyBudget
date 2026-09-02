using SimplyBudgetWeb.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimplyBudgetWeb.Core;

public static class DependencyInjection
{
    public static TBuilder AddDatabase<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStrings.DatabaseKey)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStrings.DatabaseKey}' not found.");

        void BuildDbOptions(DbContextOptionsBuilder options)
        {
            options.UseAzureSql(connectionString, sqlOptions =>
                sqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, BudgetWebContext.Schema));
        }
        builder.Services.AddDbContextFactory<BudgetWebContext>(BuildDbOptions);
        builder.Services.AddDbContextPool<BudgetWebContext>(BuildDbOptions);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        }

        if (builder.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
        {
            builder.Services.AddHostedService<DatabaseMigrationService>();
        }

        return builder;
    }

    /// <summary>
    /// Registers the post-start warm-up that primes the database connection and Entra ID metadata
    /// so the first request after a cold start doesn't have to. Enabled by default; set
    /// <c>WarmUpOnStartup</c> to <c>false</c> to disable.
    /// </summary>
    public static TBuilder AddStartupWarmup<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (builder.Configuration.GetValue("WarmUpOnStartup", defaultValue: true))
        {
            builder.Services.AddHostedService<StartupWarmupService>();
        }

        return builder;
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimplyBudget.Data;

public static class DependencyInjection
{
    public static TBuilder AddDatabase<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var connectionString = builder.Configuration.GetConnectionString(ConnectionStrings.DatabaseKey)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStrings.DatabaseKey}' not found.");

        void BuildDbOptions(DbContextOptionsBuilder options)
        {
            options.UseAzureSql(connectionString);
        }

        builder.Services.AddDbContextFactory<AppDbContext>(BuildDbOptions);
        builder.Services.AddDbContextPool<AppDbContext>(BuildDbOptions);

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
}

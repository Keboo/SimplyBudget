using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimplyBudget.Data;

/// <summary>
/// Factory used by EF Core design-time tools (dotnet ef migrations add, etc.)
/// </summary>
public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var factory = new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = false,
            ValidateScopes = false
        });
        builder.ConfigureContainer(factory);
        builder.Services.AddDbContextPool<AppDbContext>(options =>
            options.UseAzureSql(
                builder.Configuration.GetConnectionString(ConnectionStrings.DatabaseKey)
                ?? "Server=(localdb)\\mssqllocaldb;Database=SimplyBudget_Dev;Trusted_Connection=True;"));
        var host = builder.Build();
        return host.Services.GetRequiredService<AppDbContext>();
    }
}

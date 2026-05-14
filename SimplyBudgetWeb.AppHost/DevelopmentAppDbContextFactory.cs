using SimplyBudgetWeb.Core;
using SimplyBudgetWeb.Data;

using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimplyBudgetWeb.AppHost;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<BudgetWebContext>
{
    public BudgetWebContext CreateDbContext(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        var factory = new DefaultServiceProviderFactory(new ServiceProviderOptions()
        {
            ValidateOnBuild = false,
            ValidateScopes = false
        });
        builder.ConfigureContainer(factory);
        builder.AddDatabase();

        var host = builder.Build();

        return host.Services.GetRequiredService<BudgetWebContext>();
    }
}

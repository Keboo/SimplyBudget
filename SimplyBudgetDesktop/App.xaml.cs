using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.Messaging;

using MaterialDesignThemes.Wpf;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SimplyBudget.ViewModels;
using SimplyBudget.Windows;

using SimplyBudgetShared.Data;

namespace SimplyBudget;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    [STAThread]
    public static void Main(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();
        StaticDI.Register(host.Services);
        host.Start();

        App app = new();
        app.InitializeComponent();
        MigrateDatabaseAsync(host.Services.GetRequiredService<Func<BudgetContext>>()).Wait();
        app.MainWindow = host.Services.GetRequiredService<MainWindow>();
        app.MainWindow.Visibility = Visibility.Visible;
        app.Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder)
            => configurationBuilder.AddUserSecrets(typeof(App).Assembly))
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton(WeakReferenceMessenger.Default);
            services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider => provider.GetRequiredService<WeakReferenceMessenger>());

            services.AddSingleton<ICurrentMonth, CurrentMonth>();

            services.AddSingleton<IDispatcher, Dispatcher>();
            services.AddSingleton(ctx => new Func<BudgetContext>(() => new BudgetContext()));
            services.AddTransient<ISnackbarMessageQueue, SnackbarMessageQueue>();
        });

    private static async Task MigrateDatabaseAsync(Func<BudgetContext> contextFactory)
    {
        using BudgetContext context = contextFactory();
        context.Database.Migrate();

        //TODO: Async this
        if (!context.ExpenseCategories.Any())
        {
            await SampleBudget.GenerateBudget(context);
            await context.SaveChangesAsync();
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        //TODO: Resolve the dbcontext

        //DefaultAzureCredential creds = new(new DefaultAzureCredentialOptions()
        //{
        //    InteractiveBrowserTenantId = "043a9423-4a81-43ca-af56-558108fb8f06",
        //    TenantId = "043a9423-4a81-43ca-af56-558108fb8f06",

        //    ExcludeAzureCliCredential = false,
        //    ExcludeInteractiveBrowserCredential = false
        //});

#if DEBUG
        var helper = new PaletteHelper();
        var theme = helper.GetTheme();
        theme.SetPrimaryColor(Colors.Orange);
        helper.SetTheme(theme);
#endif
        base.OnStartup(e);
    }
}

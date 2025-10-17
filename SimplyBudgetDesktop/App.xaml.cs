using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Mvvm.Messaging;

using MaterialDesignThemes.Wpf;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SimplyBudget.Properties;
using SimplyBudget.ViewModels;
using SimplyBudget.Windows;

using SimplyBudgetShared.Data;
using SimplyBudgetShared.Threading;

using Velopack;

namespace SimplyBudget;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        using IHost host = CreateHostBuilder(args).Build();
        StaticDI.Register(host.Services);
        host.Start();

        App app = new();
        app.InitializeComponent();
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
            //services.AddSingleton(sp => new Func<BudgetContext>(() => new BudgetContext(Settings.GetDatabaseConnectionString())));
            services.AddSingleton(sp => new HttpClientOptions()
            {
                Endpoint = new Uri("https://localhost:5678")
            });
            services.AddScoped<IDataClient, DataClient>();
            services.AddTransient<ISnackbarMessageQueue, SnackbarMessageQueue>();
        });

    protected override void OnStartup(StartupEventArgs e)
    {
        MakeDataBackup();
        //using (var context = new BudgetContext(Settings.GetDatabaseConnectionString()))
        //{
        //    context.Database.Migrate();

        //    //TODO: Async this
        //    if (!context.ExpenseCategories.Any())
        //    {
        //        TaskEx.Run(async () =>
        //        {
        //            await SampleBudget.GenerateBudget(context);
        //            await context.SaveChangesAsync();
        //        }).Wait();
        //    }
        //}

        _ = CheckForUpdatesAsync();
#if DEBUG
        var helper = new PaletteHelper();
        var theme = helper.GetTheme();
        theme.SetPrimaryColor(Colors.Orange);
        helper.SetTheme(theme);
#endif
        base.OnStartup(e);
    }

    private static async Task<bool> CheckForUpdatesAsync()
    {
        var mgr = new UpdateManager(new Velopack.Sources.VelopackFlowSource());

        // check for new version
        var newVersion = await mgr.CheckForUpdatesAsync();
        if (newVersion is null)
            return false; // no update available

        // download new version
        await mgr.DownloadUpdatesAsync(newVersion);

        // install new version and restart app
        mgr.ApplyUpdatesAndRestart(newVersion);
        return true;
    }

    private static void MakeDataBackup()
    {
        string backupsDirectory = Path.Combine(Settings.GetStorageDirectory(), "Backups");
        DirectoryInfo backups;
        try
        {
            backups = Directory.CreateDirectory(backupsDirectory);
        }
        catch (UnauthorizedAccessException)
        {
            backupsDirectory = Path.Combine(Path.GetTempPath(), "SimplyBudget", "Backups");
            backups = Directory.CreateDirectory(backupsDirectory);
        }
        const int maxBackups = 30;
        var fileName = $"{DateTime.Now:yyyyMMddhhmmss}.db";
        try
        {
            string sourcePath = Settings.GetDatabasePath();
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, Path.Combine(backups.FullName, fileName));
            }
        }
        catch (FileNotFoundException)
        { }

        foreach (var oldBackup in backups.EnumerateFiles($"*.db")
            .OrderByDescending(x => x.Name)
            .Skip(maxBackups)
            .ToList())
        {
            try
            {
                oldBackup.Delete();
            }
            catch
            {
                //TODO: Notification
            }
        }
    }
}

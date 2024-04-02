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
using Squirrel;
using System.IO;
using System.Windows;
using System.Windows.Media;

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
            services.AddSingleton(ctx => new Func<BudgetContext>(() => new BudgetContext(Settings.GetDatabaseConnectionString())));
            services.AddTransient<ISnackbarMessageQueue, SnackbarMessageQueue>();
        });

    public static SemanticVersion? Version { get; set; }

    private static void OnAppInstall(SemanticVersion version, IAppTools tools)
    {
        tools.CreateShortcutForThisExe(ShortcutLocation.StartMenu);
    }

    private static void OnAppUninstall(SemanticVersion version, IAppTools tools)
    {
        tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenu);
    }

    private static void OnAppRun(SemanticVersion version, IAppTools tools, bool firstRun)
    {
        Version = version;
        tools.SetProcessAppUserModelId();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        SquirrelAwareApp.HandleEvents(
            onInitialInstall: OnAppInstall,
            onAppUninstall: OnAppUninstall,
            onEveryRun: OnAppRun);

#if DEBUG
        try
        {
            _ = global::Windows.ApplicationModel.Package.Current;
        }
        catch (InvalidOperationException)
        {
            //This is throw when run outside of an MSIX deployment
            Settings.Default.StorageLocation = Path.GetFullPath(@".\Database");
        }
#endif
        MakeDataBackup();
        using (var context = new BudgetContext(Settings.GetDatabaseConnectionString()))
        {
            context.Database.Migrate();

            //TODO: Async this
            if (!context.ExpenseCategories.Any())
            {
                TaskEx.Run(async () =>
                {
                    await SampleBudget.GenerateBudget(context);
                    await context.SaveChangesAsync();
                }).Wait();
            }
        }
#if DEBUG
        var helper = new PaletteHelper();
        var theme = helper.GetTheme();
        theme.SetPrimaryColor(Colors.Orange);
        helper.SetTheme(theme);
#endif
        base.OnStartup(e);
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

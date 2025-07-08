using CommunityToolkit.Mvvm.Messaging;

using SimplyBudget.Messaging;
using SimplyBudget.ViewModels;

using SimplyBudgetShared.Data;

using Velopack;

namespace SimplyBudget.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow :
    IRecipient<OpenHistory>
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        viewModel.Messenger.Register(this);

        UpdateManager updateManager = new(new Velopack.Sources.VelopackFlowSource());
        if (updateManager.IsInstalled && updateManager.CurrentVersion is { } version)
        {
            Title += $"{version.Major}.{version.Minor}.{version.Patch}";
        }
        else
        {
            Title += " - Local";
        }
        //if (App.Version is { } version)
        //{
        //    Title += $" - {version.Major}.{version.Minor}.{version.Build}";
        //}

        Loaded += MainWindow_Loaded;
        //try
        //{
        //    if (global::Windows.ApplicationModel.Package.Current?.Id?.Version is { } version)
        //    {
        //        Title += $" - {version.Major}.{version.Minor}.{version.Build}";
        //    }
        //}
        //catch (InvalidOperationException)
        //{
        //    //This is throw when run outside of an MSIX deployment
        //    Title += " - Local";
        //}
    }

    private async void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        //using var mgr = new UpdateManager(urlOrPath: null);
        //if (mgr.IsInstalledApp)
        //{
        //    try
        //    {
        //        const string channel = "production";
        //        using var remoteManager = new UpdateManager($"https://sciuridae.azurewebsites.net/Squirrel/{mgr.AppId}/{channel}");
        //        var newVersion = await remoteManager.UpdateApp();
        //
        //        // optionally restart the app automatically, or ask the user if/when they want to restart
        //        if (newVersion != null)
        //        {
        //            UpdateManager.RestartApp();
        //        }
        //    }
        //    catch
        //    {
        //        //TODO: handle failures?
        //    }
        //}
    }

    public void Receive(OpenHistory message)
    {
        var history = ((MainWindowViewModel)DataContext).History;
        ExpenseCategoryViewModelEx category = message.ExpenseCategory;
        if (!history.FilterCategories.Any(x => x.ID == category.ExpenseCategoryID) &&
            history.ExpenseCategories.FirstOrDefault(x => x.ID == category.ExpenseCategoryID) is ExpenseCategory foundCategory)
        {
            history.FilterCategories.Clear();
            history.FilterCategories.Add(foundCategory);
        }
        TabControl.SelectedIndex = 1;
    }
}

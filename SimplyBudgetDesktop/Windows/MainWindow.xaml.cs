using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using System;
using System.Linq;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow :
        IRecipient<OpenHistory>
    {
        public MainWindow()
        {
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            InitializeComponent();
            viewModel.Messenger.Register(this);

            try
            {
                if (global::Windows.ApplicationModel.Package.Current?.Id?.Version is { } version)
                {
                    Title += $" - {version.Major}.{version.Minor}.{version.Build}";
                }
            }
            catch (InvalidOperationException)
            {
                //This is throw when run outside of an MSIX deployment
                Title += " - Local";
            }
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
}

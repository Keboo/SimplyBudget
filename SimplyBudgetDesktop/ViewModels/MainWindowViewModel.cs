using AutoDI;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.ViewModels.MainWindow;
using SimplyBudgetShared.Data;
using System;

namespace SimplyBudget.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public BudgetViewModel Budget { get; }

        public HistoryViewModel History { get; }

        public MainWindowViewModel([Dependency]IMessenger messenger = null, [Dependency]BudgetContext context = null)
        {
            Budget = new BudgetViewModel(messenger ?? throw new ArgumentNullException(nameof(messenger)));
            History = new HistoryViewModel(context);

            Budget.LoadItemsAsync();
            History.LoadItemsAsync();
        }
    }
}

using AutoDI;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.ViewModels.MainWindow;
using System;

namespace SimplyBudget.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public BudgetViewModel Budget { get; }

        public MainWindowViewModel([Dependency]IMessenger messenger = null)
        {
            Budget = new BudgetViewModel(messenger ?? throw new ArgumentNullException(nameof(messenger)));

            Budget.LoadItemsAsync();
        }
    }
}

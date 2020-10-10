using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public enum AddType
    {
        None,
        Transaction,
        Income,
        Transfer
    }

    public class LineItemViewModel : ObservableObject
    {
        
    }

    public class AddItemViewModel : ObservableObject
    {
        public ICommand SubmitCommand { get; }
        public BudgetContext Context { get; }
        public IMessenger Messenger { get; }

        public ObservableCollection<LineItemViewModel> LineItems { get; }
            = new ObservableCollection<LineItemViewModel>();

        public IList<AddType> AddTypes { get; } = new List<AddType>
        {
            AddType.Transaction,
            AddType.Income,
            AddType.Transfer
        };

        private AddType _selectedType = AddType.Transaction;
        public AddType SelectedType
        {
            get => _selectedType;
            set => SetProperty(ref _selectedType, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private DateTime? _date;
        public DateTime? Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public AddItemViewModel(BudgetContext context, IMessenger messenger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            
            SubmitCommand = new RelayCommand(OnSubmit, CanSubmit);
        }

        private bool CanSubmit()
        {

            return true;
        }

        private void OnSubmit()
        {
            Messenger.Send(new ItemAddedMessage());
        }

        public class ItemAddedMessage { }
    }
}

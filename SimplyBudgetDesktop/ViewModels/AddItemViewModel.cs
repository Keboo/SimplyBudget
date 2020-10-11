using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudget.Validation;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
        public ICommand SetAmountCommand { get; }

        public IList<ExpenseCategory> ExpenseCategories { get; }

        public LineItemViewModel(IList<ExpenseCategory> expenseCategories)
        {
            ExpenseCategories = expenseCategories ?? throw new ArgumentNullException(nameof(expenseCategories));

            SetAmountCommand = new RelayCommand<int>(OnSetAmount);
        }

        private void OnSetAmount(int amount) => Amount = amount;

        private int _amount;
        public int Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        private ExpenseCategory _selectedCategory;
        public ExpenseCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }
    }

    public class AddItemViewModel : ValidationViewModel
    {
        public ICommand SubmitCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand CancelCommand { get; }
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

        private AddType _selectedType;
        public AddType SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value))
                {
                    LineItems.Clear();
                    switch(value)
                    {
                        case AddType.Transaction:
                            LineItems.Add(new LineItemViewModel(ExpenseCategories));
                            break;
                        case AddType.Income:
                            LineItems.AddRange(ExpenseCategories.Select(x => new LineItemViewModel(ExpenseCategories)
                            {
                                SelectedCategory = x
                            }));
                            break;
                    }
                }
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private DateTime? _date;
        [ReasonableDate]
        public DateTime? Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        private IList<ExpenseCategory> ExpenseCategories { get; }

        public AddItemViewModel(BudgetContext context, IMessenger messenger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            SubmitCommand = new AsyncRelayCommand(OnSubmit, CanSubmit);
            AddItemCommand = new RelayCommand(OnAddItem);
            RemoveItemCommand = new RelayCommand<LineItemViewModel>(OnRemoveItem);
            CancelCommand = new RelayCommand(OnCancel);

            ExpenseCategories = context.ExpenseCategories.OrderBy(x => x.Name).ToList();

            SelectedType = AddType.Transaction;
        }

        private void OnCancel()
        {
            Messenger.Send(new DoneAddingItemMessage());
        }

        private void OnRemoveItem(LineItemViewModel item)
        {
            if (SelectedType == AddType.Transaction && LineItems.Count > 1)
            {
                LineItems.Remove(item);
            }
        }

        private void OnAddItem()
        {
            LineItems.Add(new LineItemViewModel(ExpenseCategories));
        }

        private bool CanSubmit()
        {

            return true;
        }

        private async Task OnSubmit()
        {
            bool result = SelectedType switch
            {
                AddType.Transaction => await TrySubmitTransaction(),
                _ => false
            };

            if (result)
            {
                Messenger.Send(new DoneAddingItemMessage());
            }
        }

        private async Task<bool> TrySubmitTransaction()
        {
            if (Date is null) return false;

            var items = GetValidLineItems().ToList();
            if (!items.Any()) return false;

            await Context.AddTransaction(Description, Date.Value, items.Select(vm => (vm.Amount, vm.SelectedCategory.ID)).ToArray());

            return true;
        }

        private IEnumerable<LineItemViewModel> GetValidLineItems() 
            => LineItems.Where(x => x.SelectedCategory != null && x.Amount > 0);
    }

}

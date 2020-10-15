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
    public class LineItemViewModel : ObservableObject
    {
        public ICommand SetAmountCommand { get; }

        public IList<ExpenseCategory> ExpenseCategories { get; }

        public LineItemViewModel(IList<ExpenseCategory> expenseCategories)
        {
            ExpenseCategories = expenseCategories ?? throw new ArgumentNullException(nameof(expenseCategories));

            SetAmountCommand = new RelayCommand(OnSetAmount);
        }

        private void OnSetAmount()
        {
            Amount = DesiredAmount;
        }

        private int _amount;
        public int Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }


        private int _desiredAmount;
        public int DesiredAmount
        {
            get => _desiredAmount;
            set => SetProperty(ref _desiredAmount, value);
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
                            LoadDesiredAmounts();
                            break;
                        case AddType.Transfer:
                            LineItems.Add(new LineItemViewModel(ExpenseCategories));
                            LineItems.Add(new LineItemViewModel(ExpenseCategories) { DesiredAmount = -1 });
                            break;
                    }
                }
            }
        }

        private int _totalAmount;
        public int TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (SetProperty(ref _totalAmount, value))
                {
                    if (SelectedType == AddType.Income)
                    {
                        foreach(var lineItem in LineItems.Where(x => x.SelectedCategory?.UsePercentage == true))
                        {
                            lineItem.DesiredAmount = (int)(value * lineItem.SelectedCategory.BudgetedPercentage / 100.0);
                        }
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

            SubmitCommand = new AsyncRelayCommand(OnSubmit);
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

        private async void LoadDesiredAmounts()
        {
            foreach (var lineItem in LineItems)
            {
                lineItem.DesiredAmount = await Context.GetRemainingBudgetAmount(lineItem.SelectedCategory, DateTime.Today);
            }
        }

        private async Task OnSubmit()
        {
            bool result = SelectedType switch
            {
                AddType.Transaction => await TrySubmitTransaction(),
                AddType.Income => await TrySubmitIncome(),
                AddType.Transfer => await TrySubmitTransfer(),
                _ => false
            };

            if (result)
            {
                Messenger.Send(new DoneAddingItemMessage());
            }
        }

        private async Task<bool> TrySubmitTransfer()
        {
            if (Date is null) return false;
            if (TotalAmount <= 0) return false;

            var items = LineItems.Where(x => x.SelectedCategory != null).ToList();
            if (items.Count != 2) return false;

            await Context.AddTransfer(Description, Date.Value, TotalAmount, items[0].SelectedCategory, items[1].SelectedCategory);

            return true;
        }

        private async Task<bool> TrySubmitIncome()
        {
            if (Date is null) return false;
            if (TotalAmount <= 0) return false;

            var items = GetValidLineItems().ToList();
            if (items.Sum(x => x.Amount) != TotalAmount) return false;

            await Context.AddIncome(Description, Date.Value, items.Select(x => (x.Amount, x.SelectedCategory.ID)).ToArray());

            return true;
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

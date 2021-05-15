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
using System.Windows.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class AddItemViewModel : ValidationViewModel,
        IRecipient<LineItemAmountUpdated>,
        IRecipient<CurrentMonthChanged>
    {
        public ICommand SubmitCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveItemCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AutoAllocateCommand { get; }
        public BudgetContext Context { get; }
        public ICurrentMonth CurrentMonth { get; }
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
                    TotalAmount = 0;
                    UpdateRemaining();

                    switch (value)
                    {
                        case AddType.Transaction:
                            LineItems.Add(new LineItemViewModel(ExpenseCategories, Messenger));
                            break;
                        case AddType.Income:
                            LineItems.AddRange(ExpenseCategories
                                .Select(x => new LineItemViewModel(ExpenseCategories, Messenger)
                                {
                                    SelectedCategory = x
                                })
                                .OrderByDescending(x => x.SelectedCategory?.UsePercentage)
                                .ThenBy(x => x.SelectedCategory?.Name));
                            LoadDesiredAmounts();
                            break;
                        case AddType.Transfer:
                            LineItems.Add(new LineItemViewModel(ExpenseCategories, Messenger));
                            LineItems.Add(new LineItemViewModel(ExpenseCategories, Messenger) { DesiredAmount = -1 });
                            Date = DateTime.Today;
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
                        foreach (var lineItem in LineItems.Where(x => x.SelectedCategory?.UsePercentage == true))
                        {
                            lineItem.DesiredAmount = (int)(value * lineItem.SelectedCategory!.BudgetedPercentage / 100.0);
                        }
                    }
                    UpdateRemaining();
                }
            }
        }

        private int _remainingAmount;
        public int RemainingAmount
        {
            get => _remainingAmount;
            private set => SetProperty(ref _remainingAmount, value);
        }

        private string? _description;
        public string? Description
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

        public IList<ExpenseCategory> ExpenseCategories { get; }

        public AddItemViewModel(BudgetContext context, ICurrentMonth currentMonth, IMessenger messenger)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            Messenger.Register<LineItemAmountUpdated>(this);
            Messenger.Register<CurrentMonthChanged>(this);

            SubmitCommand = new AsyncRelayCommand(OnSubmit);
            AddItemCommand = new RelayCommand(OnAddItem);
            RemoveItemCommand = new RelayCommand<LineItemViewModel>(OnRemoveItem);
            CancelCommand = new RelayCommand(OnCancel);
            AutoAllocateCommand = new RelayCommand(OnAutoAllocate);

            ExpenseCategories = context.ExpenseCategories.OrderBy(x => x.Name).ToList();

            SelectedType = AddType.Transaction;

            PropertyChanged += (_, __) => ClearValidationErrors(nameof(SubmitCommand));
            BindingOperations.EnableCollectionSynchronization(LineItems, new object());
        }

        private void OnCancel()
            => Messenger.Send(new DoneAddingItemMessage());

        private void OnRemoveItem(LineItemViewModel? item)
        {
            if (SelectedType == AddType.Transaction &&
                LineItems.Count > 1 &&
                item is not null)
            {
                LineItems.Remove(item);
                TotalAmount = LineItems.Sum(x => x.Amount);
            }
        }

        private void OnAddItem()
            => LineItems.Add(new LineItemViewModel(ExpenseCategories, Messenger));

        private void OnAutoAllocate()
        {
            int total = RemainingAmount;
            foreach (var lineItem in LineItems.OrderByDescending(x => x.SelectedCategory?.UsePercentage))
            {
                int desiredAmount = lineItem.DesiredAmount;
                if (lineItem.SelectedCategory?.UsePercentage == true)
                {
                    desiredAmount = (int)(TotalAmount * (lineItem.SelectedCategory.BudgetedPercentage / 100.0));
                }
                lineItem.Amount = Math.Min(total, desiredAmount);
                total -= lineItem.Amount;
            }
        }

        private async void LoadDesiredAmounts()
        {
            foreach (var lineItem in LineItems.Where(x => x.SelectedCategory is not null))
            {
                var desiredAmount = await Context.GetRemainingBudgetAmount(lineItem.SelectedCategory!, CurrentMonth.CurrenMonth);
                lineItem.DesiredAmount = desiredAmount;
                lineItem.SetAmountCallback = x =>
                {
                    if (x.SelectedCategory?.UsePercentage == true)
                    {
                        return (int)Math.Round(x.SelectedCategory.BudgetedPercentage / 100.0 * TotalAmount);
                    }
                    return Math.Min(RemainingAmount, desiredAmount);
                };
            }
        }

        private async Task OnSubmit()
        {
            var errors = await (SelectedType switch
            {
                AddType.Transaction => TrySubmitTransaction(),
                AddType.Income => TrySubmitIncome(),
                AddType.Transfer => TrySubmitTransfer(),
                _ => throw new InvalidOperationException()
            }).ToListAsync();

            SetValidationErrors(nameof(SubmitCommand), errors);

            if (!errors.Any())
            {
                Messenger.Send(new DoneAddingItemMessage());
            }
        }

        private void UpdateRemaining()
        {
            RemainingAmount = TotalAmount - LineItems.Sum(x => x.Amount);
        }

        private async IAsyncEnumerable<string> TrySubmitTransfer()
        {
            if (Date is null)
            {
                yield return "Date is required";
                yield break;
            }
            if (TotalAmount <= 0)
            {
                yield return "Total amount is required";
                yield break;
            }

            var items = LineItems.Where(x => x.SelectedCategory != null).ToList();
            if (items.Count != 2)
            {
                yield return "Both a From and To categories must be defined";
                yield break;
            }

            if (items[0].SelectedCategory?.ID == items[1].SelectedCategory?.ID)
            {
                yield return "From and To categories must be different";
                yield break;
            }

            await Context.AddTransfer(Description ?? "", Date.Value, TotalAmount,
                items[0].SelectedCategory!, items[1].SelectedCategory!);
        }

        private async IAsyncEnumerable<string> TrySubmitIncome()
        {
            if (Date is null)
            {
                yield return "Date is required";
                yield break;
            }
            if (TotalAmount <= 0)
            {
                yield return "Total amount is required";
                yield break;
            }

            var items = GetValidLineItems().ToList();
            if (items.Sum(x => x.Amount) != TotalAmount)
            {
                yield return "All income must be allocated to a category";
                yield break;
            }

            if (items.FirstOrDefault(x => x.Amount < 0) is { } negativeItem)
            {
                yield return $"{negativeItem.SelectedCategory?.Name ?? "<empty>"} has a negative amount";
                yield break;
            }

            await Context.AddIncome(Description ?? "", Date.Value, items.Select(x => (x.Amount, x.SelectedCategory!.ID)).ToArray());
        }

        private async IAsyncEnumerable<string> TrySubmitTransaction()
        {
            if (Date is null)
            {
                yield return "Date is required";
                yield break;
            }

            var items = GetValidLineItems().ToList();
            if (!items.Any())
            {
                yield return "At least one line item must be completed";
                yield break;
            }

            await Context.AddTransaction(Description ?? "", Date.Value, items.Select(vm => (vm.Amount, vm.SelectedCategory!.ID)).ToArray());
        }

        private IEnumerable<LineItemViewModel> GetValidLineItems()
            => LineItems.Where(x => x.SelectedCategory != null && x.Amount > 0);

        public void Receive(LineItemAmountUpdated message)
        {
            switch (SelectedType)
            {
                case AddType.Income:
                    UpdateRemaining();
                    break;
                case AddType.Transaction:
                    TotalAmount = LineItems.Sum(x => x.Amount);
                    break;
            }
        }

        public void Receive(CurrentMonthChanged message)
        {
            Task.Run(() =>
            {
                ValidateModel();
                if (SelectedType == AddType.Income)
                {
                    SelectedType = AddType.None;
                    SelectedType = AddType.Income;
                }
            });
        }
    }
}

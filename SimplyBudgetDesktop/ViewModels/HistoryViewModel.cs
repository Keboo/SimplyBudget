using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Events;
using SimplyBudgetShared.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SimplyBudget.ViewModels.MainWindow
{
    public class HistoryViewModel : CollectionViewModelBase<BudgetHistoryViewModel>,
        IRecipient<TransactionEvent>,
        IRecipient<TransactionItemEvent>,
        IRecipient<IncomeEvent>,
        IRecipient<IncomeItemEvent>,
        IRecipient<TransferEvent>,
        IRecipient<CurrentMonthChanged>
    {
        public BudgetContext Context { get; }
        public ICurrentMonth CurrentMonth { get; }

        public IRelayCommand AddFilterCommand { get; }
        public ICommand RemoveFilterCommand { get; }

        public ObservableCollection<Account> Accounts { get; }
            = new ObservableCollection<Account>();

        public ObservableCollection<ExpenseCategory> ExpenseCategories { get; }
            = new ObservableCollection<ExpenseCategory>();

        public ObservableCollection<ExpenseCategory> FilterCategories { get; }
            = new ObservableCollection<ExpenseCategory>();

        private ExpenseCategory _selectedCategory;
        public ExpenseCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    AddFilterCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private Account _selectedAccount;
        public Account SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (SetProperty(ref _selectedAccount, value))
                {
                    LoadItemsAsync();
                }
            }
        }

        public HistoryViewModel(BudgetContext context, IMessenger messenger, ICurrentMonth currentMonth)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            CurrentMonth = currentMonth ?? throw new ArgumentNullException(nameof(currentMonth));

            ExpenseCategories.AddRange(context.ExpenseCategories);

            AddFilterCommand = new RelayCommand<ExpenseCategory>(OnAddFilter, x => x != null);
            RemoveFilterCommand = new RelayCommand<ExpenseCategory>(
                x => FilterCategories.Remove(x), x => x != null);

            FilterCategories.CollectionChanged += FilterCategories_CollectionChanged;

            messenger.Register<TransactionEvent>(this);
            messenger.Register<TransactionItemEvent>(this);
            messenger.Register<IncomeEvent>(this);
            messenger.Register<IncomeItemEvent>(this);
            messenger.Register<TransferEvent>(this);
            messenger.Register<CurrentMonthChanged>(this);
        }

        private void OnAddFilter(ExpenseCategory category)
        {
            if (!FilterCategories.Contains(category))
            {
                FilterCategories.Add(category);
            }
            SelectedCategory = null;
        }

        private void FilterCategories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => LoadItemsAsync();

        protected override async IAsyncEnumerable<BudgetHistoryViewModel> GetItems()
        {
            var oldestTime = CurrentMonth.CurrenMonth.AddMonths(-2).StartOfMonth();

            int currentAmount = 0;
            var categoryList = new List<int>();
            if (FilterCategories.Any())
            {
                categoryList.AddRange(FilterCategories.Select(x => x.ID));
            }
            else
            {
                currentAmount = Context.ExpenseCategoryItemDetails
                    .Include(x => x.ExpenseCategory)
                    .Where(x => x.ExpenseCategory.AccountID == SelectedAccount.ID)
                    .Sum(x => x.Amount);
            }

            var query = Context.ExpenseCategoryItems
                .Include(x => x.Details)
                .ThenInclude(x => x.ExpenseCategory)
                .Where(x => x.Date >= oldestTime);

            if (categoryList.Any())
            {
                query = query.Where(x => x.Details.Any(x => categoryList.Contains(x.ExpenseCategoryId)));
            }

            query = query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.ID);

            await foreach (var item in query.AsAsyncEnumerable())
            {
                yield return new BudgetHistoryViewModel(item, currentAmount);
                if (!FilterCategories.Any())
                {
                    currentAmount -= item.Details
                        .Where(x => x.ExpenseCategory.AccountID == SelectedAccount.ID)
                        .Sum(x => x.Amount);
                }
            }
        }

        protected override async Task ReloadItemsAsync()
        {
            int? selectedId = SelectedAccount?.ID;
            Accounts.Clear();
            Accounts.AddRange(Context.Accounts);
            _selectedAccount = Accounts.FirstOrDefault(x => x.ID == selectedId) ??
                await Context.GetDefaultAccountAsync();
            await base.ReloadItemsAsync();
            OnPropertyChanged(nameof(SelectedAccount));
        }

        public async Task DeleteItems(IEnumerable<BudgetHistoryViewModel> items)
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items.ToList())
            {
                await item.Delete(Context);
            }
        }

        public void Receive(IncomeItemEvent message) => LoadItemsAsync();

        public void Receive(IncomeEvent message) => LoadItemsAsync();

        public void Receive(TransactionEvent message) => LoadItemsAsync();

        public void Receive(TransactionItemEvent message) => LoadItemsAsync();

        public void Receive(TransferEvent message) => LoadItemsAsync();

        public void Receive(CurrentMonthChanged message) => LoadItemsAsync();
    }
}
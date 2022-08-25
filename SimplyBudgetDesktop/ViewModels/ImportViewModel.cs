using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Import;
using SimplyBudgetShared.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Data;

namespace SimplyBudget.ViewModels;

public class ImportViewModel : ObservableObject
{
    public IAsyncRelayCommand ImportCommand { get; }
    public IRelayCommand AddItemCommand { get; }
    public IRelayCommand DeleteCommand { get; }
    public IRelayCommand MarkAsDoneCommand { get; }
    public IRelayCommand UnmarkAsDoneCommand { get; }
    public Func<BudgetContext> ContextFactory { get; }
    public IMessenger Messenger { get; }

    public ObservableCollection<ImportItem> ImportedRecords { get; } = new();

    private bool _IsViewingCsv = true;
    public bool IsViewingCsv
    {
        get => _IsViewingCsv;
        set => SetProperty(ref _IsViewingCsv, value);
    }

    private string? _CsvData;
    public string? CsvData
    {
        get => _CsvData;
        set => SetProperty(ref _CsvData, value);
    }

    public int SelectedAmount => SelectedItems?.Sum(x => x.Item.Details?.Sum(x => x.Amount) ?? 0) ?? 0;

    private IList<ImportItem>? _SelectedItems;
    public IList<ImportItem>? SelectedItems
    {
        get => _SelectedItems;
        set
        {
            if (SetProperty(ref _SelectedItems, value))
            {
                base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedAmount)));
                AddItemCommand.NotifyCanExecuteChanged();
                DeleteCommand.NotifyCanExecuteChanged();
                MarkAsDoneCommand.NotifyCanExecuteChanged();
                UnmarkAsDoneCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public ImportViewModel(Func<BudgetContext> contextFactory, IMessenger messenger)
    {
        ImportCommand = new AsyncRelayCommand(OnImport);
        AddItemCommand = new RelayCommand(OnAddItem, CanAddItem);
        DeleteCommand = new RelayCommand(OnDeleteItem, () => SelectedItems?.Any() == true);
        MarkAsDoneCommand = new RelayCommand(OnMarkAsDone, () => SelectedItems?.Any(x => x.IsDone == false) == true);
        UnmarkAsDoneCommand = new RelayCommand(OnUnmarkAsDone, () => SelectedItems?.Any(x => x.IsDone) == true);

        BindingOperations.EnableCollectionSynchronization(ImportedRecords, new object());
        ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
    }

    private bool CanAddItem()
    {
        if (SelectedItems?.Any() != true) return false;
        return SelectedItems.All(x => x.Amount >= 0) || SelectedItems.All(x => x.Amount <= 0);
    }

    private async void OnAddItem()
    {
        var selectedItems = SelectedItems;
        if (selectedItems is null) return;

        var type = selectedItems.All(x => x.Item.Details?.Sum(d => d.Amount) > 0) ? AddType.Income : AddType.Transaction;
        var date = selectedItems.Min(x => x.Date.Date);
        var description = string.Join(Environment.NewLine, selectedItems.Select(x => x.Description));
        var items = selectedItems.Select(x => new LineItem(x.Amount)).ToList();

        SelectedItems = null;

        await TaskEx.Run(() =>
        {
            foreach (var selectedItem in selectedItems)
            {
                selectedItem.IsDone = true;
            }

            var message = new AddItemMessage(type, date, description, items);
            Messenger.Send(message);
        });
    }

    private async Task OnImport()
    {
        if (string.IsNullOrWhiteSpace(CsvData)) return;

        await Task.Yield();

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvData));
        IImport importer = new StcuCsvImport(stream);
        using var context = ContextFactory();

        List<ImportItem> records = new();
        await foreach (ExpenseCategoryItem item in importer.GetItems())
        {
            bool? isDone = null;
            foreach(var detail in item.Details ?? Enumerable.Empty<ExpenseCategoryItemDetail>())
            {
                isDone = await context.ExpenseCategoryItemDetails
                    .Include(x => x.ExpenseCategoryItem)
                    .Where(x => x.ExpenseCategoryItem!.Date == item.Date)
                    .AnyAsync(x => x.Amount == detail.Amount);
                if (isDone == false) break;
            }
            if (isDone != true)
            {
                decimal? expectedTotal = item.Details?.Sum(x => x.Amount);
                await foreach(var eci in context.ExpenseCategoryItems
                    .Include(x => x.Details)
                    .Where(x => x.Date == item.Date)
                    .AsAsyncEnumerable())
                {
                    if (eci.Details?.Sum(d => d.Amount) == expectedTotal)
                    {
                        isDone = true;
                    }
                }
            }
            records.Add(new ImportItem(item) { IsDone = isDone ?? false });
        }

        ImportedRecords.Clear();
        foreach (var record in records.OrderBy(x => x.Date))
        {
            ImportedRecords.Add(record);
        }

        IsViewingCsv = false;
    }

    private void OnDeleteItem()
    {
        foreach (var item in SelectedItems?.ToList() ?? Enumerable.Empty<ImportItem>())
        {
            ImportedRecords.Remove(item);
        }
    }

    private void OnMarkAsDone()
    {
        foreach (var item in SelectedItems ?? Enumerable.Empty<ImportItem>())
        {
            item.IsDone = true;
        }
    }

    private void OnUnmarkAsDone()
    {
        foreach (var item in SelectedItems ?? Enumerable.Empty<ImportItem>())
        {
            item.IsDone = false;
        }
    }
}

public class ImportItem : ObservableObject
{
    public ExpenseCategoryItem Item { get; }
    public ImportItem(ExpenseCategoryItem item)
    {
        Item = item;
    }

    public int Amount => Math.Abs(Item.Details?.Sum(x => x.Amount) ?? 0);

    public string? Description => Item.Description;

    public DateTime Date => Item.Date;

    private bool _isDone;
    public bool IsDone
    {
        get => _isDone;
        set => SetProperty(ref _isDone, value);
    }
}

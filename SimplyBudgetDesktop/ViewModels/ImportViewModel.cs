using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Import;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class ImportViewModel : ObservableObject
    {
        public ICommand ImportCommand { get; }
        public IRelayCommand AddItemCommand { get; }
        public ICommand DeleteCommand { get; }
        public IMessenger Messenger { get; }

        public ObservableCollection<ImportRecord> ImportedRecords { get; } = new();

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

        private IList<ImportRecord>? _SelectedItems;
        public IList<ImportRecord>? SelectedItems
        {
            get => _SelectedItems;
            set
            {
                if (SetProperty(ref _SelectedItems, value))
                {
                    base.OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedAmount)));
                    AddItemCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public ImportViewModel(IMessenger messenger)
        {
            ImportCommand = new RelayCommand(OnImport);
            AddItemCommand = new RelayCommand(OnAddItem, CanAddItem);
            DeleteCommand = new RelayCommand(OnDeleteItem);

            BindingOperations.EnableCollectionSynchronization(ImportedRecords, new object());
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

            await Task.Run(() =>
            {
                var type = selectedItems.All(x => x.Item.Details?.Sum(d => d.Amount) > 0) ? AddType.Income : AddType.Transaction;
                var date = selectedItems.Min(x => x.Date.Date);
                var description = string.Join(Environment.NewLine, selectedItems.Select(x => x.Description));
                var items = selectedItems.Select(x => new LineItem(x.Amount)).ToList();

                foreach (var selectedItem in selectedItems)
                {
                    ImportedRecords.Remove(selectedItem);
                }

                var message = new AddItemMessage(type, date, description, items);
                Messenger.Send(message);
            });
            SelectedItems = null;
        }

        private async void OnImport()
        {
            if (string.IsNullOrWhiteSpace(CsvData)) return;

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(CsvData));
            IImport importer = new StcuCsvImport(stream);

            List<ImportRecord> records = new();
            await foreach (ExpenseCategoryItem item in importer.GetItems())
            {
                records.Add(new ImportRecord(item));
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
            foreach(var item in SelectedItems?.ToList() ?? Enumerable.Empty<ImportRecord>())
            {
                ImportedRecords.Remove(item);
            }
        }
    }

    public record ImportRecord(ExpenseCategoryItem Item)
    {
        public int Amount => Math.Abs(Item.Details?.Sum(x => x.Amount) ?? 0);

        public string? Description => Item.Description;

        public DateTime Date => Item.Date;
    }
}

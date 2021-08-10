using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Import;
using SimplyBudgetShared.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class ImportViewModel : ObservableObject
    {
        public ICommand ImportCommand { get; }
        public IRelayCommand AddItemCommand { get; }
        public IRelayCommand DeleteCommand { get; }
        public IRelayCommand MarkAsDoneCommand { get; }
        public IRelayCommand UnmarkAsDoneCommand { get; }

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
                    DeleteCommand.NotifyCanExecuteChanged();
                    MarkAsDoneCommand.NotifyCanExecuteChanged();
                    UnmarkAsDoneCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public ImportViewModel(IMessenger messenger)
        {
            ImportCommand = new RelayCommand(OnImport);
            AddItemCommand = new RelayCommand(OnAddItem, CanAddItem);
            DeleteCommand = new RelayCommand(OnDeleteItem, () => SelectedItems?.Any() == true);
            MarkAsDoneCommand = new RelayCommand(OnMarkAsDone, () => SelectedItems?.Any(x => x.IsDone == false) == true);
            UnmarkAsDoneCommand = new RelayCommand(OnUnmarkAsDone, () => SelectedItems?.Any(x => x.IsDone) == true);

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
            foreach (var item in SelectedItems?.ToList() ?? Enumerable.Empty<ImportRecord>())
            {
                ImportedRecords.Remove(item);
            }
        }

        private void OnMarkAsDone()
        {
            foreach (var item in SelectedItems ?? Enumerable.Empty<ImportRecord>())
            {
                item.IsDone = true;
            }
        }

        private void OnUnmarkAsDone()
        {
            foreach (var item in SelectedItems ?? Enumerable.Empty<ImportRecord>())
            {
                item.IsDone = false;
            }
        }
    }

    public class ImportRecord : INotifyPropertyChanged
    {
        public ExpenseCategoryItem Item { get; }
        public ImportRecord(ExpenseCategoryItem item)
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

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

using Microsoft.Toolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    internal abstract class CollectionViewModelBase : ViewModelBase
    {
        public abstract void LoadItemsAsync();

        public abstract void UnloadItems();
    }

    internal abstract class CollectionViewModelBase<T> : CollectionViewModelBase
    {
        protected readonly ObservableCollection<T> _items;
        protected readonly ICollectionView _view;
        private readonly RelayCommand<string> _sortCommand;

        private long _loadInProgress;

        protected CollectionViewModelBase()
        {
            _items = new ObservableCollection<T>();
            _view = CollectionViewSource.GetDefaultView(_items);
            _sortCommand = new RelayCommand<string>(OnSort);
        }

        private void OnSort(string sortProperty)
        {
            if (string.IsNullOrWhiteSpace(sortProperty)) return;

            var existingSortDescriptor = _view.SortDescriptions.FirstOrDefault(x => x.PropertyName == sortProperty);

            if (existingSortDescriptor != default(SortDescription))
            {
                var index = _view.SortDescriptions.IndexOf(existingSortDescriptor);
                _view.SortDescriptions.RemoveAt(index);
                _view.SortDescriptions.Insert(index,
                                              new SortDescription(sortProperty,
                                                                  existingSortDescriptor.Direction == ListSortDirection.Ascending
                                                                      ? ListSortDirection.Descending
                                                                      : ListSortDirection.Ascending));
            }
            else
            {
                if (_view.SortDescriptions.Count > 0)
                    _view.SortDescriptions.RemoveAt(_view.SortDescriptions.Count - 1);
                _view.SortDescriptions.Add(new SortDescription(sortProperty, ListSortDirection.Ascending));
            }
        }

        public override void UnloadItems()
        {
            _items.Clear();
        }

        public override async void LoadItemsAsync()
        {
            await ReloadItemsAsync();
        }

        public ICommand SortCommand => _sortCommand;

        protected abstract Task<IEnumerable<T>> GetItems();

        protected async Task ReloadItemsAsync()
        {
            var currentId = Interlocked.Increment(ref _loadInProgress);
            var items = await GetItems();
            if (Interlocked.Read(ref _loadInProgress) == currentId)
            {
                _items.Clear();
                if (items != null)
                    foreach (var item in items)
                        _items.Add(item);
            }
        }
    }
}
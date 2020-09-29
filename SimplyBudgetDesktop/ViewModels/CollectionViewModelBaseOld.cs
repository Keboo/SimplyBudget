using Microsoft.Toolkit.Mvvm.Input;
using System;
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
    public abstract class CollectionViewModelBaseOld<T> : CollectionViewModelBase
    {
        protected readonly ObservableCollection<T> _items;
        protected readonly ICollectionView _view;
        private readonly RelayCommand<string> _sortCommand;

        private long _loadInProgress;

        protected CollectionViewModelBaseOld()
        {
            _items = new ObservableCollection<T>();
            _view = CollectionViewSource.GetDefaultView(_items);
            _sortCommand = new RelayCommand<string>(OnSort);
        }

        private void OnSort(string sortProperty)
        {
            if (string.IsNullOrWhiteSpace(sortProperty)) return;

            Sort(sortProperty);
        }

        protected void Sort(string propertyName, ListSortDirection? sortDirection = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            
            var existingSortDescriptor = _view.SortDescriptions.FirstOrDefault(x => x.PropertyName == propertyName);

            if (existingSortDescriptor != default)
            {
                var index = _view.SortDescriptions.IndexOf(existingSortDescriptor);

                sortDirection ??= existingSortDescriptor.Direction == ListSortDirection.Ascending
                                                                      ? ListSortDirection.Descending
                                                                      : ListSortDirection.Ascending;

                _view.SortDescriptions.RemoveAt(index);
                _view.SortDescriptions.Insert(index,
                                              new SortDescription(propertyName, sortDirection.Value));
            }
            else
            {
                if (_view.SortDescriptions.Count > 0)
                    _view.SortDescriptions.RemoveAt(_view.SortDescriptions.Count - 1);
                _view.SortDescriptions.Add(new SortDescription(propertyName, sortDirection ?? ListSortDirection.Ascending));
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
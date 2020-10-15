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
    public abstract class CollectionViewModelBase : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
    {
        public ICommand RefreshCommand { get; }

        protected CollectionViewModelBase()
        {
            RefreshCommand = new RelayCommand(OnRefresh);
        }

        private void OnRefresh() => LoadItemsAsync();

        public abstract void LoadItemsAsync();

        public abstract void UnloadItems();
    }

    public abstract class CollectionViewModelBase<T> : CollectionViewModelBase
    {
        protected ObservableCollection<T> Items { get; }
        protected readonly ICollectionView _view;
        private readonly RelayCommand<string> _sortCommand;
        private SemaphoreSlim LoadLock { get; } = new SemaphoreSlim(1);
        protected CollectionViewModelBase()
        {
            Items = new ObservableCollection<T>();
            BindingOperations.EnableCollectionSynchronization(Items, new object());
            _view = CollectionViewSource.GetDefaultView(Items);
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
            Items.Clear();
        }

        public override async void LoadItemsAsync()
        {
            await ReloadItemsAsync();
        }

        public ICommand SortCommand => _sortCommand;

        protected abstract IAsyncEnumerable<T> GetItems();

        protected virtual async Task ReloadItemsAsync()
        {
            await LoadLock.WaitAsync();
            try
            {
                Items.Clear();

                await foreach (var item in GetItems())
                {
                    Items.Add(item);
                }
            }
            finally
            {
                LoadLock.Release();
            }
        }
    }
}
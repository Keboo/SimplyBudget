using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;

namespace SimplyBudget.ViewModels;

public abstract partial class CollectionViewModelBase : ObservableObject
{
    protected CollectionViewModelBase() { }

    [RelayCommand]
    private Task OnRefreshAsync() => LoadItemsAsync();

    public abstract Task LoadItemsAsync();

    public abstract void UnloadItems();
}

public abstract partial class CollectionViewModelBase<T> : CollectionViewModelBase
{
    public ObservableCollection<T> Items { get; }
    protected readonly ICollectionView _view;
    private SemaphoreSlim LoadLock { get; } = new SemaphoreSlim(1);
    protected CollectionViewModelBase()
    {
        Items = new ObservableCollection<T>();
        BindingOperations.EnableCollectionSynchronization(Items, new object());
        _view = CollectionViewSource.GetDefaultView(Items);

        Items.CollectionChanged += Items_CollectionChanged;
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        //NB: Volatile read of the semaphore count.
        if (LoadLock.CurrentCount <= 0) return;
        foreach (var item in e.OldItems?.OfType<T>() ?? Enumerable.Empty<T>())
        {
            ItemRemoved(item);
        }
        foreach (var item in e.NewItems?.OfType<T>() ?? Enumerable.Empty<T>())
        {
            ItemAdded(item);
        }
    }

    [RelayCommand]
    private void OnSort(string? sortProperty)
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

    public override async Task LoadItemsAsync()
    {
        await ReloadItemsAsync();
    }

    protected abstract IAsyncEnumerable<T> GetItems();

    protected virtual void ItemAdded(T item) { }
    protected virtual void ItemRemoved(T item) { }

    protected virtual async Task ReloadItemsAsync()
    {
        await LoadLock.WaitAsync().ConfigureAwait(false);
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
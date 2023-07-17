using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudget.ViewModels;

namespace SimplyBudgetDesktop.Tests.ViewModels;

[TestClass]
public class CollectionViewModelBaseTests
{
    [TestMethod]
    public async Task LoadItemsAsync_LoadsItems()
    {
        TestableCollectionViewModel<int> sut = new(1, 2, 3);

        await sut.LoadItemsAsync();

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, sut.Items);
    }

    [TestMethod]
    public async Task AddingItemToCollection_CallsItemAdded()
    {
        TestableCollectionViewModel<int> sut = new(1, 2, 3);
        await sut.LoadItemsAsync();

        sut.Items.Add(4);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, sut.Items);
        CollectionAssert.AreEquivalent(new[] { 4 }, sut.AddedItems.ToArray());
    }

    [TestMethod]
    public async Task RemovingItemFromCollection_CallsItemRemoved()
    {
        TestableCollectionViewModel<int> sut = new(1, 2, 3);
        await sut.LoadItemsAsync();

        sut.Items.Remove(2);

        CollectionAssert.AreEquivalent(new[] { 1, 3 }, sut.Items);
        CollectionAssert.AreEquivalent(new[] { 2 }, sut.RemovedItems.ToArray());
    }

    [TestMethod]
    public async Task ReplacingItemInCollection_CallsItemRemovedAndItemAdded()
    {
        TestableCollectionViewModel<int> sut = new(1, 2, 3);
        await sut.LoadItemsAsync();

        sut.Items[1] = 5;

        CollectionAssert.AreEquivalent(new[] { 1, 5, 3 }, sut.Items);
        CollectionAssert.AreEquivalent(new[] { 2 }, sut.RemovedItems.ToArray());
        CollectionAssert.AreEquivalent(new[] { 5 }, sut.AddedItems.ToArray());
    }

    private class TestableCollectionViewModel<T> : CollectionViewModelBase<T>
    {
        public TestableCollectionViewModel(params T[] items)
        {
            TestItems = items;
        }

        public T[] TestItems { get; }

        public IList<T> AddedItems { get; } = new List<T>();
        public IList<T> RemovedItems { get; } = new List<T>();

        protected override IAsyncEnumerable<T> GetItems()
        {
            return TestItems.ToAsyncEnumerable();
        }

        protected override void ItemAdded(T item)
        {
            AddedItems.Add(item);
        }

        protected override void ItemRemoved(T item)
        {
            RemovedItems.Add(item);
        }
    }
}

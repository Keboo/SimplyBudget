using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace SimplyBudget.Utilities
{
    public class ItemsStack<T> : ObservableCollection<T>
    {
        private readonly Stack<ICollection<T>> _collectionsStack;

        private bool _blockCollectionChanged;

        public ItemsStack()
        {
            _collectionsStack = new Stack<ICollection<T>>();
        }

        public int CollectionsCount => _collectionsStack.Count;

        public void PushCollection(ICollection<T> collection)
        {
            if (collection is null) throw new ArgumentNullException("collection");
            _collectionsStack.Push(collection);

            _blockCollectionChanged = true;

            Clear();
            foreach(var item in collection)
                Add(item);

            _blockCollectionChanged = false;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs("CollectionsCount"));
        }

        public ICollection<T>? PopCollection()
        {
            if (_collectionsStack.Count == 0)
                return null;

            var rv = _collectionsStack.Pop();

            _blockCollectionChanged = true;

            Clear();

            if (_collectionsStack.Count > 0)
            {
                var collection = _collectionsStack.Peek();
                foreach(var item in collection)
                    Add(item);
            }

            _blockCollectionChanged = false;

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs("CollectionsCount"));
            return rv;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (_blockCollectionChanged == false)
                base.OnCollectionChanged(e);
        }
    }
}
using System.Collections.ObjectModel;

namespace SimplyBudget.Utilities
{
    public class ObservqbleCollectionEx<T> : ObservableCollection<T>
    {
        public bool ItemsAdded { get; set; }
    }
}
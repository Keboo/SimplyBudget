
using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace SimplyBudget.Behaviors
{
    //Based on an example from: http://compositewpf.codeplex.com/SourceControl/changeset/view/69631#1007338
    public class SyncSelectedItemsBehavior : Behavior<ListBox>
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(
                "SelectedItems",
                typeof(IList),
                typeof(SyncSelectedItemsBehavior),
                new PropertyMetadata(null, OnSelectedItemsPropertyChanged));

        private static void OnSelectedItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (SyncSelectedItemsBehavior) d;
            var list = e.NewValue as IList;
            if (list != null)
            {
                var listBox = behavior.AssociatedObject;
                if (listBox != null)
                {
                    listBox.SelectionChanged -= behavior.OnSelectedItemsChanged;
                    foreach (var item in list)
                        listBox.SelectedItems.Add(item);
                    
                    list.Clear();
                    
                    foreach (var selectedItem in listBox.SelectedItems)
                        list.Add(selectedItem);
                    
                    listBox.SelectionChanged += behavior.OnSelectedItemsChanged;
                }
            }
        }

        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.SelectionChanged += OnSelectedItemsChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= OnSelectedItemsChanged;

            base.OnDetaching();
        }

        private void OnSelectedItemsChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelections(e);
        }

        private void UpdateSelections(SelectionChangedEventArgs e)
        {
            var selections = SelectedItems;
            if (selections is null) return;
            
            foreach (var item in e.AddedItems)
            {
                selections.Add(item);
            }

            foreach (var item in e.RemovedItems)
            {
                selections.Remove(item);
            }
        }
    }
}
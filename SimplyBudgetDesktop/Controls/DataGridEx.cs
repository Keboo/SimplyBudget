using SimplyBudget.Utilities;
using SimplyBudgetShared.Utilities;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace SimplyBudget.Controls
{
    //Based on an example at: http://bengribaudo.com/blog/2012/03/14/1942/saving-restoring-wpf-datagrid-columns-size-sorting-and-order
    public class DataGridEx : DataGrid
    {
        public event EventHandler<EventArgs>? ColumnsResized;
        public event EventHandler<DataGridSortingEventArgs>? AfterColumnSorted;

        private readonly DelayAction _delayColumnWidthAction;

        public DataGridEx()
        {
            _delayColumnWidthAction = new DelayAction();
            _delayColumnWidthAction.Action += (sender, e) => ColumnsResized?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            base.OnSorting(eventArgs);
            if (eventArgs.Handled == false)
                AfterColumnSorted?.Invoke(this, eventArgs);
        }

        protected override void OnInitialized(EventArgs e)
        {
            EventHandler widthPropertyChangedHandler = (sender, args) => _delayColumnWidthAction.RaiseAction();
            var widthPropertyDescriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));

            Loaded += (sender, x) =>
            {
                foreach (var column in Columns)
                {
                    widthPropertyDescriptor.AddValueChanged(column, widthPropertyChangedHandler);
                }
            };

            Unloaded += (sender, x) =>
            {
                foreach (var column in Columns)
                {
                    widthPropertyDescriptor.RemoveValueChanged(column, widthPropertyChangedHandler);
                }
            };

            base.OnInitialized(e);
        }
    }
}
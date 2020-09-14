using SimplyBudget.Controls;
using SimplyBudget.Properties;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace SimplyBudget.Behaviors
{
    public class PersistenDataGridBehavior : Behavior<DataGridEx>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.ColumnReordered += DataGridOnColumnReordered;
            AssociatedObject.ColumnsResized += DataGridOnColumnsResized;
            AssociatedObject.AfterColumnSorted += DataGridOnAfterColumnSorted;
            
            if(AssociatedObject.IsLoaded)
                LoadSettings();
            else
            {
                RoutedEventHandler loadedHandler = null;
                loadedHandler += (sender, e) =>
                                     {
                                         AssociatedObject.Loaded -= loadedHandler;
                                         LoadSettings();
                                     };
                AssociatedObject.Loaded += loadedHandler;
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.ColumnReordered -= DataGridOnColumnReordered;
            AssociatedObject.ColumnsResized -= DataGridOnColumnsResized;
            AssociatedObject.AfterColumnSorted -= DataGridOnAfterColumnSorted;
            
            base.OnDetaching();
        }

        private void DataGridOnColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            SaveSettings();
        }

        private void DataGridOnColumnsResized(object sender, EventArgs eventArgs)
        {
            SaveSettings();
        }

        private void DataGridOnAfterColumnSorted(object sender, DataGridSortingEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            DataGridSettingsManager.SaveSettings(AssociatedObject);
        }

        private void LoadSettings()
        {
            var settings = DataGridSettingsManager.GetSettings(AssociatedObject);
            if (settings != null)
                settings.LoadSettings(AssociatedObject);
        }
    }
}
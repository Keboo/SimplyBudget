using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using SimplyBudget.ViewModels;

namespace SimplyBudget.Views;

/// <summary>
/// Interaction logic for ImportView.xaml
/// </summary>
public partial class ImportView
{
    private ImportViewModel ViewModel => (ImportViewModel)DataContext;

    public ImportView()
    {
        InitializeComponent();
        DataGrid.SelectionChanged += DataGrid_SelectionChanged;
        DataContextChanged += ImportView_DataContextChanged;
    }

    private void ImportView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ImportViewModel oldVM)
        {
            oldVM.PropertyChanged -= ViewModel_PropertyChanged;
        }
        if (e.NewValue is ImportViewModel newVM)
        {
            //newVM.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ImportViewModel.SelectedItems):
                IList<ImportItem>? selectedItems = ViewModel.SelectedItems;
                if (!selectedItems?.SequenceEqual(DataGrid.SelectedItems.OfType<ImportItem>()) != true)
                {
                    DataGrid.SelectionChanged -= DataGrid_SelectionChanged;
                    DataGrid.SelectedItems.Clear();
                    foreach (var item in selectedItems ?? Enumerable.Empty<ImportItem>())
                    {
                        DataGrid.SelectedItems.Add(item);
                    }
                    DataGrid.SelectionChanged += DataGrid_SelectionChanged;
                }
                break;
        }
    }

    private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.SelectedItems = DataGrid.SelectedItems.OfType<ImportItem>().ToArray();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                try
                {
                    ViewModel.CsvData = File.ReadAllText(file);
                    break;
                }
                catch (IOException)
                { 
                    //TODO: Show some UI indicator indicating the failure
                }
            }
        }
    }

    private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }
}

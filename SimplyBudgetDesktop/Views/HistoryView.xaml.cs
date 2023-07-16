using SimplyBudget.ViewModels;
using System.Linq;
using System.Windows.Controls;

namespace SimplyBudget.Views;

/// <summary>
/// Interaction logic for HistoryView.xaml
/// </summary>
public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
    }

    private async void DeleteSelectedHistoryItemExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
    {
        var vm = (HistoryViewModel)HistoryListView.DataContext;
        await vm.DeleteItems(HistoryListView.SelectedItems.OfType<BudgetHistoryViewModel>());
    }
}

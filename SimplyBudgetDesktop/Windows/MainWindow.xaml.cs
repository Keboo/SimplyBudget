using SimplyBudget.ViewModels;
using SimplyBudget.ViewModels.MainWindow;
using System.Linq;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        public MainWindow()
        {
            DataContext = new MainWindowViewModel();
            InitializeComponent();
        }

        private async void DeleteSelectedHistoryItemExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var vm = (HistoryViewModel)HistoryListView.DataContext;
            await vm.DeleteItems(HistoryListView.SelectedItems.OfType<BudgetHistoryViewModel>());
        }
    }
}

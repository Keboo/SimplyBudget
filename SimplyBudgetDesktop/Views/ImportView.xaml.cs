using SimplyBudget.ViewModels;
using System.Linq;
using System.Windows.Controls;

namespace SimplyBudget.Views
{
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
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectedItems = DataGrid.SelectedItems.OfType<ImportRecord>().ToArray();
        }
    }
}

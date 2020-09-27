using SimplyBudget.ViewModels;

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
    }
}

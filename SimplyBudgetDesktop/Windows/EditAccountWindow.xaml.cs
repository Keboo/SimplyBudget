using SimplyBudget.ViewModels.Windows;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for EditAccountWindow.xaml
    /// </summary>
    partial class EditAccountWindow 
    {
        public EditAccountWindow()
        {
            InitializeComponent();
        }

        public EditAccountViewModel ViewModel
        {
            get { return (EditAccountViewModel) DataContext; }
        }
    }
}

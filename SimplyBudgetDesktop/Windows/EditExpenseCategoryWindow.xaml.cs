using System.Windows;
using System.Windows.Input;
using SimplyBudget.ViewModels.Windows;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for EditExpenseCategoryWindow.xaml
    /// </summary>
    partial class EditExpenseCategoryWindow 
    {
        public EditExpenseCategoryWindow()
        {
            InitializeComponent();
            RoutedEventHandler loadedHandler = null;
            loadedHandler = (sender, e) =>
            {
                Loaded -= loadedHandler;
                MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            };
            Loaded += loadedHandler;
        }

        public EditExpenseCategoryViewModel ViewModel
        {
            get { return (EditExpenseCategoryViewModel) DataContext; }
        }
    }
}

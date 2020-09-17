
using System.Windows;
using System.Windows.Input;
using SimplyBudget.ViewModels.Windows;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for EditIncomeItemWindow.xaml
    /// </summary>
    partial class EditIncomeItemWindow 
    {
        public EditIncomeItemWindow()
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

        public EditIncomeItemViewModel ViewModel => (EditIncomeItemViewModel)DataContext;
    }
}

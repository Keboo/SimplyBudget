using System.Windows;
using System.Windows.Input;
using SimplyBudget.ViewModels.Windows;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for EditTransactionWindow.xaml
    /// </summary>
    partial class EditTransactionWindow
    {
        public EditTransactionWindow()
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

        public EditTransactionViewModel ViewModel => (EditTransactionViewModel)DataContext;
    }
}

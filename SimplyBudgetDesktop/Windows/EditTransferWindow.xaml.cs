
using System.Windows;
using System.Windows.Input;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for EditTransferWindow.xaml
    /// </summary>
    public partial class EditTransferWindow
    {
        public EditTransferWindow()
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
    }
}

using System.Windows.Input;
using SimplyBudget.Properties;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class OldMainWindow
    {
        public OldMainWindow()
        {
            InitializeComponent();
        }

        private void CloseCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            Settings.Default.Save();
            base.OnClosed(e);
        }
    }
}

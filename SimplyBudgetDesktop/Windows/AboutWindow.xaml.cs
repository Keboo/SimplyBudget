

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace SimplyBudget.Windows
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    partial class AboutWindow
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void OKButtonClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void EmailLinkOnClick(object sender, RoutedEventArgs e)
        {
            var hyperlink = (Hyperlink)sender;
            try
            {
                Process.Start(hyperlink.NavigateUri.ToString());
            }
            catch
            {
            }
        }
    }
}

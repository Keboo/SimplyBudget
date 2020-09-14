using System;
using System.Windows;
using SimplyBudget.ViewModels;

namespace SimplyBudget.Windows
{
    public class BaseWindow : Window
    {
         protected BaseWindow()
         {
             DataContextChanged += OnDataContextChanged;
         }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var requstCloseContext = e.OldValue as IRequestClose;
            if (requstCloseContext != null)
                requstCloseContext.RequestClose -= OnRequestClose;
            requstCloseContext = e.NewValue as IRequestClose;
            if (requstCloseContext != null)
                requstCloseContext.RequestClose += OnRequestClose;
        }

        private void OnRequestClose(object sender, EventArgs eventArgs)
        {
            Close();
        }
    }
}
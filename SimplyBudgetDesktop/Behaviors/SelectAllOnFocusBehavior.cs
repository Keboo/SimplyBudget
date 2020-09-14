using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace SimplyBudget.Behaviors
{
    public class SelectAllOnFocusBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            DoSelection();
            AssociatedObject.GotFocus += OnGotFocus;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.GotFocus -= OnGotFocus;
            base.OnDetaching();
        }

        private void OnGotFocus(object sender, RoutedEventArgs routedEventArgs)
        {
            DoSelection();
        }

        private void DoSelection()
        {
            if (AssociatedObject.IsFocused)
                AssociatedObject.SelectAll();
        }
    }
}
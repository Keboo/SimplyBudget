using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace SimplyBudget.Behaviors
{
    public class FocusOnVisibleBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            DoFocus();
            AssociatedObject.IsVisibleChanged += OnIsVisibleChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.IsVisibleChanged -= OnIsVisibleChanged;
            base.OnDetaching();
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DoFocus();
        }

        private void DoFocus()
        {
            if (AssociatedObject.Focusable)
                AssociatedObject.Focus();
        }
    }
}
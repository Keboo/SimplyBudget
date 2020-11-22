using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace SimplyBudget.Behaviors
{
    public class FocusOnVisibleChangedBehavior : Behavior<TextBox>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject.IsLoaded)
                AssociatedObject.IsVisibleChanged += OnIsVisibleChanged;
            else
            {
                RoutedEventHandler? loadedHandler = null;
                loadedHandler = (sender, e) =>
                                    {
                                        AssociatedObject.Loaded -= loadedHandler;
                                        AssociatedObject.IsVisibleChanged += OnIsVisibleChanged;
                                    };
                AssociatedObject.Loaded += loadedHandler;
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.IsVisibleChanged -= OnIsVisibleChanged;
            base.OnDetaching();
        }

        private void OnIsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
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
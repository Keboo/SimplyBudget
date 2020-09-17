using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace SimplyBudget.Behaviors
{
    public class CommandOnKeyPressBehavior : Behavior<DependencyObject>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(CommandOnKeyPressBehavior),
            new PropertyMetadata(default(ICommand)));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register("CommandParameter", typeof(object), typeof(CommandOnKeyPressBehavior),
            new PropertyMetadata(default(object)));

        public object CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public Key Key { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            var uiElement = AssociatedObject as UIElement;
            if (uiElement != null)
                uiElement.KeyDown += OnKeyDown;
            else
            {
                var contentElement = AssociatedObject as ContentElement;
                if (contentElement != null)
                    contentElement.KeyDown += OnKeyDown;
            }
        }

        protected override void OnDetaching()
        {
            var uiElement = AssociatedObject as UIElement;
            if (uiElement != null)
                uiElement.KeyDown -= OnKeyDown;
            else
            {
                var contentElement = AssociatedObject as ContentElement;
                if (contentElement != null)
                    contentElement.KeyDown -= OnKeyDown;
            }
            base.OnDetaching();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key)
            {
                var command = Command;
                var parameter = CommandParameter;
                if (command != null && command.CanExecute(parameter))
                    command.Execute(parameter);
            }
        }
    }
}
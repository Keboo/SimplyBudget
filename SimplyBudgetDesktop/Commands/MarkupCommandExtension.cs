using System;
using System.Windows.Input;
using System.Windows.Markup;

namespace SimplyBudget.Commands
{
    [MarkupExtensionReturnType(typeof(ICommand))]
    public abstract class MarkupCommandExtension<T> : MarkupExtension, ICommand, IRaiseCanExecute where T : ICommand, new()
    {
        public event EventHandler CanExecuteChanged;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new T();
        }

        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public abstract void Execute(object parameter);

        public void NotifyCanExecuteChanged()
        {
            var @event = CanExecuteChanged;
            if (@event != null)
                @event(this, EventArgs.Empty);
        }
    }
}
using SimplyBudget.Windows;

namespace SimplyBudget.Commands
{
    public class ShowAddAccountCommand : MarkupCommandExtension<ShowAddAccountCommand>
    {
        // ReSharper disable EmptyConstructor
        public ShowAddAccountCommand() { }
        // ReSharper restore EmptyConstructor

        public override void Execute(object parameter)
        {
            new EditAccountWindow().Show();
        }
    }
}
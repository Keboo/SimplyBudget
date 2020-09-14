using SimplyBudget.Windows;

namespace SimplyBudget.Commands
{
    public class ShowAddTransactionCommand : MarkupCommandExtension<ShowAddTransactionCommand>
    {
        // ReSharper disable EmptyConstructor
        public ShowAddTransactionCommand() { }
        // ReSharper restore EmptyConstructor

        public override void Execute(object parameter)
        {
            new EditTransactionWindow().Show();
        }
    }
}
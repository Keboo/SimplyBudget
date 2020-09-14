using SimplyBudget.Windows;

namespace SimplyBudget.Commands
{
    public class ShowAddTransferCommand : MarkupCommandExtension<ShowAddTransferCommand>
    {
        // ReSharper disable EmptyConstructor
        public ShowAddTransferCommand() { }
        // ReSharper restore EmptyConstructor

        public override void Execute(object parameter)
        {
            new EditTransferWindow().Show();
        }
    }
}
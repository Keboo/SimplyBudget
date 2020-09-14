using SimplyBudget.Windows;

namespace SimplyBudget.Commands
{
    public class ShowAddExpenseCategoryCommand : MarkupCommandExtension<ShowAddExpenseCategoryCommand>
    {
        // ReSharper disable EmptyConstructor
        public ShowAddExpenseCategoryCommand() { }
        // ReSharper restore EmptyConstructor

        public override void Execute(object parameter)
        {
            new EditExpenseCategoryWindow().Show();
        }
    }
}
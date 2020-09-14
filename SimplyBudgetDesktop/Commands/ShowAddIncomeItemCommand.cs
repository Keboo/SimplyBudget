using SimplyBudget.Windows;

namespace SimplyBudget.Commands
{
    public class ShowAddIncomeItemCommand : MarkupCommandExtension<ShowAddIncomeItemCommand>
    {
        // ReSharper disable EmptyConstructor
        public ShowAddIncomeItemCommand() { }
        // ReSharper restore EmptyConstructor

        public override async void Execute(object parameter)
        {
            var window = new EditIncomeItemWindow();
            await window.ViewModel.LoadDefaultView();
            window.Show();
        }
    }
}
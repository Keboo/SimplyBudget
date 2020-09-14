using SimplyBudget.Windows;

namespace SimplyBudget.Commands
{
    public class ShowSettingsCommand : MarkupCommandExtension<ShowSettingsCommand>
    {
        // ReSharper disable EmptyConstructor
        public ShowSettingsCommand() { }
        // ReSharper restore EmptyConstructor

        public override void Execute(object parameter)
        {
            new SettingsWindow().Show();
        }
    }
}
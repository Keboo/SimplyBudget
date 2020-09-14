using SimplyBudget.Utilities;
using SimplyBudget.Windows;

namespace SimplyBudget.Commands
{
    public class ShowAboutWindowCommand : MarkupCommandExtension<ShowAboutWindowCommand>
    {
        // ReSharper disable EmptyConstructor
        public ShowAboutWindowCommand() { }
        // ReSharper restore EmptyConstructor

        public override void Execute(object parameter)
        {
            SingletonWindow.ShowWindow<AboutWindow>();
        }
    }
}
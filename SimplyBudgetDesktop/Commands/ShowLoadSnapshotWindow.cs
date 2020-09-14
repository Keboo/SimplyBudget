using SimplyBudget.Utilities;
using SimplyBudget.Windows;

namespace SimplyBudget.Commands
{
    public class ShowLoadSnapshotWindow : MarkupCommandExtension<ShowLoadSnapshotWindow>
    {
        // ReSharper disable EmptyConstructor
        public ShowLoadSnapshotWindow() { }
        // ReSharper restore EmptyConstructor
        
        public override void Execute(object parameter)
        {
            SingletonWindow.ShowWindow<LoadSnapshotWindow>();    
        }
    }
}
using SimplyBudget.Utilities;
using System;
using System.Windows;

namespace SimplyBudget.Commands
{
    public class CreateSnapshotCommand : MarkupCommandExtension<CreateSnapshotCommand>
    {
        // ReSharper disable EmptyConstructor
        public CreateSnapshotCommand() { }
        // ReSharper restore EmptyConstructor

        public override async void Execute(object parameter)
        {
            try
            {
                await SnapshotManager.Instance.CreateSnapshotAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error creating snapshot\n" + ex.Message);
            }
        }
    }
}
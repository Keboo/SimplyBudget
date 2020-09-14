using SimplyBudget.ViewModels.Data;
using SimplyBudgetShared.Data;

namespace SimplyBudget.Commands
{
    public class DeleteDatabaseItemCommand : MarkupCommandExtension<DeleteDatabaseItemCommand>
    {
        // ReSharper disable EmptyConstructor
        public DeleteDatabaseItemCommand() { }
        // ReSharper restore EmptyConstructor

        public override async void Execute(object parameter)
        {
            var databaseItem = parameter as BaseItem;
            if (databaseItem != null)
                await databaseItem.Delete();
            else
            {
                var dbItem = parameter as IDatabaseItem;
                if (dbItem != null)
                {
                    var item = await dbItem.GetItem();
                    if (item != null)
                        await item.Delete();
                }
            }
        }

        public override bool CanExecute(object parameter)
        {
            return parameter as BaseItem != null || parameter as IDatabaseItem != null;
        }
    }
}
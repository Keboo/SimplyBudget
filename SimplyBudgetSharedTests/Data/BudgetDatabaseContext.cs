using IntelliTect.TestTools.Data;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudgetShared.Data;

namespace SimplyBudgetSharedTests.Data
{
    public class BudgetDatabaseContext : DatabaseFixture<BudgetContext>
    {
        public IMessenger Messenger { get; } = new WeakReferenceMessenger();

        public BudgetDatabaseContext()
        {
            AddDependency(Messenger);
        }
    }
}

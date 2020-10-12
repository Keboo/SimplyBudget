using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudget.ViewModels;
using SimplyBudgetSharedTests.Data;
using System.Threading.Tasks;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class MainWindowViewModelTests
    {
        [TestMethod]
        public void Constructor_CreatesDependencies()
        {
            var fixture = new BudgetDatabaseContext();
            var messenger = new WeakReferenceMessenger();

            fixture.PerformDatabaseOperation(context =>
            {
                var vm = new MainWindowViewModel(messenger, context);
                Assert.IsNotNull(vm.Budget);
                Assert.IsNotNull(vm.History);
                Assert.IsNotNull(vm.Accounts);

                return Task.CompletedTask;
            });
        }
    }
}

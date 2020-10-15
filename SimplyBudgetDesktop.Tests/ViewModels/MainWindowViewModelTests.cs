using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.Messaging;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
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

        [TestMethod]
        [DataRow(null, AddType.Transaction)]
        [DataRow(AddType.None, AddType.Transaction)]
        [DataRow(AddType.Income, AddType.Income)]
        [DataRow(AddType.Transaction, AddType.Transaction)]
        [DataRow(AddType.Transfer, AddType.Transfer)]
        public void AddCommands_SetsAddItem(AddType? parameter, AddType expected)
        {
            var mocker = new AutoMocker().WithMessenger();
            using var scope = mocker.BeginDbScope();

            var vm = mocker.CreateInstance<MainWindowViewModel>();

            Assert.IsTrue(vm.ShowAddCommand.CanExecute(parameter));
            vm.ShowAddCommand.Execute(parameter);

            Assert.IsNotNull(vm.AddItem);
            Assert.AreEqual(expected, vm.AddItem.SelectedType);
        }

        [TestMethod]
        public void OnDoneAddingItemMessage_AddItemCleared()
        {
            var mocker = new AutoMocker().WithMessenger();
            using var scope = mocker.BeginDbScope();

            var vm = mocker.CreateInstance<MainWindowViewModel>();
            vm.AddItem = mocker.CreateInstance<AddItemViewModel>();

            IMessenger messenger = mocker.Get<IMessenger>();
            messenger.Send(new DoneAddingItemMessage());

            Assert.IsNull(vm.AddItem);
        }
    }
}

using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.Messaging;
using SimplyBudget.ViewModels;
using SimplyBudgetSharedTests;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class MainWindowViewModelTests
    {
        [TestMethod]
        public void Constructor_CreatesDependencies()
        {
            var mocker = new AutoMocker().WithMessenger();
            using var scope = mocker.WithDbScope();

            var vm = mocker.CreateInstance<MainWindowViewModel>();
            Assert.IsNotNull(vm.Budget);
            Assert.IsNotNull(vm.History);
            Assert.IsNotNull(vm.Accounts);
        }

        [TestMethod]
        [DataRow(null, AddType.Transaction)]
        [DataRow(AddType.None, AddType.Transaction)]
        [DataRow(AddType.Income, AddType.Income)]
        [DataRow(AddType.Transaction, AddType.Transaction)]
        [DataRow(AddType.Transfer, AddType.Transfer)]
        public void AddCommands_SetsAddItem(AddType? parameter, AddType expected)
        {
            var mocker = new AutoMocker()
                .WithDefaults();
            using var scope = mocker.WithDbScope();
            using var _ = mocker.WithAutoDIResolver();

            var vm = mocker.CreateInstance<MainWindowViewModel>();

            Assert.IsTrue(vm.ShowAddCommand.CanExecute(parameter));
            vm.ShowAddCommand.Execute(parameter);

            Assert.IsNotNull(vm.AddItem);
            Assert.AreEqual(expected, vm.AddItem?.SelectedType);
        }

        [TestMethod]
        public void OnDoneAddingItemMessage_AddItemCleared()
        {
            var mocker = new AutoMocker().WithMessenger();
            using var scope = mocker.WithDbScope();

            var vm = mocker.CreateInstance<MainWindowViewModel>();
            vm.AddItem = mocker.CreateInstance<AddItemViewModel>();

            IMessenger messenger = mocker.Get<IMessenger>();
            messenger.Send(new DoneAddingItemMessage());

            Assert.IsNull(vm.AddItem);
        }
    }
}

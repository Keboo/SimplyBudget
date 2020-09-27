using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudget.ViewModels;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class MainWindowViewModelTests
    {
        [TestMethod]
        public void Constructor_CreatesDependencies()
        {
            var messenger = new Messenger();
            var vm = new MainWindowViewModel(messenger);

            Assert.IsNotNull(vm.Budget);
        }
    }
}

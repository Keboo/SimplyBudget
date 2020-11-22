using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SimplyBudget;
using SimplyBudget.ViewModels.MainWindow;
using SimplyBudgetShared.Data;
using System;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class BudgetViewModelTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithoutContext_Throws()
        {
            new BudgetViewModel(null!, Mock.Of<IMessenger>(), Mock.Of<ICurrentMonth>());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithoutMessenger_Throws()
        {
            new BudgetViewModel(Mock.Of<BudgetContext>(), null!, Mock.Of<ICurrentMonth>());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithoutCurrentMonth_Throws()
        {
            new BudgetViewModel(Mock.Of<BudgetContext>(), Mock.Of<IMessenger>(), null!);
        }
    }
}

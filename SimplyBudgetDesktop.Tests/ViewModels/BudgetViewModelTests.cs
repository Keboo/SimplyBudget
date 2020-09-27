using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudget.ViewModels.MainWindow;
using System;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class BudgetViewModelTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithoutMessenger_Throws()
        {
            new BudgetViewModel(null);
        }
    }
}

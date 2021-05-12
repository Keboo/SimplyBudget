using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using System;
using System.Threading.Tasks;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class AddItemViewModelTests
    {
        [TestMethod]
        public async Task SelectedType_Transaction_LoadsItems()
        {
            var mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.BeginDbScope();

            var context = mocker.Get<BudgetContext>();
            var category1 = new ExpenseCategory { Name = "Foo" };
            var category2 = new ExpenseCategory { Name = "Bar" };
            context.AddRange(category1, category2);
            await context.SaveChangesAsync();

            var vm = mocker.CreateInstance<AddItemViewModel>();

            vm.SelectedType = AddType.Transaction;

            Assert.AreEqual(1, vm.LineItems.Count);
            Assert.IsNull(vm.LineItems[0].SelectedCategory);
        }

        [TestMethod]
        public async Task SelectedType_Income_LoadsItems()
        {
            var mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.BeginDbScope();

            var context = mocker.Get<BudgetContext>();
            var category1 = new ExpenseCategory { Name = "Foo" };
            var category2 = new ExpenseCategory { Name = "Bar" };
            var category3 = new ExpenseCategory { Name = "Cat" };
            context.AddRange(category1, category2, category3);
            await context.SaveChangesAsync();

            var vm = mocker.CreateInstance<AddItemViewModel>();

            vm.SelectedType = AddType.Income;

            Assert.AreEqual(3, vm.LineItems.Count);
            Assert.AreEqual(category2, vm.LineItems[0].SelectedCategory);
            Assert.AreEqual(category3, vm.LineItems[1].SelectedCategory);
            Assert.AreEqual(category1, vm.LineItems[2].SelectedCategory);
        }

        [TestMethod]
        public async Task SelectedType_Transfer_LoadsItems()
        {
            var mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.BeginDbScope();

            var context = mocker.Get<BudgetContext>();
            var category1 = new ExpenseCategory { Name = "Foo" };
            var category2 = new ExpenseCategory { Name = "Bar" };
            var category3 = new ExpenseCategory { Name = "Cat" };
            context.AddRange(category1, category2, category3);
            await context.SaveChangesAsync();

            var vm = mocker.CreateInstance<AddItemViewModel>();

            vm.SelectedType = AddType.Transfer;

            Assert.AreEqual(2, vm.LineItems.Count);
            Assert.IsNull(vm.LineItems[0].SelectedCategory);
            Assert.IsNull(vm.LineItems[1].SelectedCategory);
            Assert.AreEqual(DateTime.Today, vm.Date);
        }

        [TestMethod]
        public void RemovingItem_UpdatesRemainingAmount()
        {
            var mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.BeginDbScope();

            var vm = mocker.CreateInstance<AddItemViewModel>();

            vm.SelectedType = AddType.Transaction;
            vm.AddItemCommand.Execute(null);
            vm.LineItems[0].Amount = 300;
            vm.LineItems[1].Amount = 400;

            vm.RemoveItemCommand.Execute(vm.LineItems[0]);

            Assert.AreEqual(400, vm.TotalAmount);
        }

        [TestMethod]
        public async Task AutoAllocateCommand_AllocatesIncomeAmount()
        {
            var mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.BeginDbScope();

            var context = mocker.Get<BudgetContext>();
            var category1 = new ExpenseCategory { Name = "Bar", BudgetedPercentage = 10 };
            var category2 = new ExpenseCategory { Name = "Cat", BudgetedAmount = 100 };
            var category3 = new ExpenseCategory { Name = "Foo", BudgetedAmount = 200 };
            context.AddRange(category1, category2, category3);
            await context.SaveChangesAsync();

            var vm = mocker.CreateInstance<AddItemViewModel>();

            vm.SelectedType = AddType.Income;
            vm.TotalAmount = 200;

            vm.AutoAllocateCommand.Execute(null);

            Assert.AreEqual(3, vm.LineItems.Count);
            Assert.AreEqual(20, vm.LineItems[0].Amount);
            Assert.AreEqual(100, vm.LineItems[1].Amount);
            Assert.AreEqual(80, vm.LineItems[2].Amount);
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;
using System;
using System.Linq;
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
            using var _ = mocker.WithDbScope();

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
            using var _ = mocker.WithDbScope();

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
            using var _ = mocker.WithDbScope();
            using var __ = mocker.WithAutoDIResolver();

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
            using var _ = mocker.WithDbScope();

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
            using var _ = mocker.WithDbScope();

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

        [TestMethod]
        [DataRow(-3)]
        [DataRow(2)]
        public void Date_MonthsOutOfRange_ShowsWarning(int monthOffset)
        {
            var now = DateTime.Now;
            var mocker = new AutoMocker().WithDefaults();
            using var __ = mocker.WithAutoDIResolver();
            using var _ = mocker.WithDbScope();

            var vm = mocker.CreateInstance<AddItemViewModel>();

            vm.Date = DateTime.Now.AddMonths(monthOffset);

            var errors = vm.GetErrors(nameof(vm.Date)).OfType<string>().ToList();
            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual($"Date should be between {now.AddMonths(-2).StartOfMonth():d} and {now.AddMonths(1).EndOfMonth():d}", errors[0]);
        }

        [TestMethod]
        [DataRow(-2)]
        [DataRow(0)]
        [DataRow(1)]
        public void Date_MonthsWithinRange_DoesNotShowWarning(int monthOffset)
        {
            var mocker = new AutoMocker().WithDefaults();
            using var __ = mocker.WithAutoDIResolver();
            using var _ = mocker.WithDbScope();

            var vm = mocker.CreateInstance<AddItemViewModel>();

            vm.Date = DateTime.Now.AddMonths(monthOffset);

            var errors = vm.GetErrors(nameof(vm.Date)).OfType<string>().ToList();
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        [Description("Issue 14")]
        public void Date_ChangingCurrentMonth_ClearsWarning()
        {
            var mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.WithDbScope();
            using var __ = mocker.WithAutoDIResolver();
            using var ___ = mocker.WithSynchonousTaskRunner();

            ICurrentMonth current = mocker.Get<ICurrentMonth>();

            var vm = mocker.CreateInstance<AddItemViewModel>();
            current.CurrenMonth = DateTime.Now.AddMonths(-10);

            vm.Date = DateTime.Now;
            var errorsBefore = vm.GetErrors(nameof(vm.Date)).OfType<string>().ToList();
            current.CurrenMonth = DateTime.Now;
            var errorsAfter = vm.GetErrors(nameof(vm.Date)).OfType<string>().ToList();

            Assert.AreEqual(1, errorsBefore.Count);
            Assert.AreEqual(0, errorsAfter.Count);
        }
    }
}

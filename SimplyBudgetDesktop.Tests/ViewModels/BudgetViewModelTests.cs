using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using SimplyBudgetSharedTests;
using System.Threading.Tasks;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class BudgetViewModelTests
    {
        [TestMethod]
        public void ConstructorThrowsExpectedExceptions()
            => ConstructorTest.AssertArgumentNullExceptions<BudgetViewModel>();

        [TestMethod]
        public async Task SaveChanges_CategoryNotFound_ReturnsFalse()
        {
            AutoMocker mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.WithDbScope();

            var vm = mocker.CreateInstance<BudgetViewModel>();
            var expenseCategory = await ExpenseCategoryViewModelEx.Create(mocker.Get<Func<BudgetContext>>(), new ExpenseCategory { ID = 42 });

            bool result = await vm.SaveChanges(expenseCategory);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task SaveChanges_WithFoundCategory_UpdatesBudgettedAmount()
        {
            AutoMocker mocker = new AutoMocker().WithDefaults();
            using var factory = mocker.WithDbScope();
            using var context = factory.Create();
            context.ExpenseCategories.Add(new ExpenseCategory
            {
                ID = 42,
                CategoryName = "CategoryName",
                Name = "Name",
                BudgetedAmount = 100
            });
            await context.SaveChangesAsync();

            var vm = mocker.CreateInstance<BudgetViewModel>();
            var expenseCategory = await ExpenseCategoryViewModelEx.Create(mocker.Get<Func<BudgetContext>>(), new ExpenseCategory { ID = 42 });
            expenseCategory.EditingCategory = "CategoryNameChanged";
            expenseCategory.EditingName = "NameChanged";
            expenseCategory.EditAmount = 120;
            expenseCategory.EditIsAmountType = true;

            bool result = await vm.SaveChanges(expenseCategory);

            Assert.IsTrue(result);
            using var verificationContext = factory.Create();
            var existing = await verificationContext.ExpenseCategories.FindAsync(42);
            Assert.AreEqual("CategoryNameChanged", existing.CategoryName);
            Assert.AreEqual("NameChanged", existing.Name);
            Assert.AreEqual(120, existing.BudgetedAmount);
            Assert.AreEqual(0, existing.BudgetedPercentage);
        }

        [TestMethod]
        public async Task SaveChanges_WithFoundCategory_UpdatesBudgettedPercentage()
        {
            AutoMocker mocker = new AutoMocker().WithDefaults();
            using var factory = mocker.WithDbScope();
            using var context = factory.Create();
            context.ExpenseCategories.Add(new ExpenseCategory
            {
                ID = 42,
                CategoryName = "CategoryName",
                Name = "Name",
                BudgetedAmount = 100
            });
            await context.SaveChangesAsync();

            var vm = mocker.CreateInstance<BudgetViewModel>();
            var expenseCategory = await ExpenseCategoryViewModelEx.Create(mocker.Get<Func<BudgetContext>>(), new ExpenseCategory { ID = 42 });
            expenseCategory.EditingCategory = "CategoryNameChanged";
            expenseCategory.EditingName = "NameChanged";
            expenseCategory.EditAmount = 10;
            expenseCategory.EditIsAmountType = false;

            bool result = await vm.SaveChanges(expenseCategory);

            Assert.IsTrue(result);
            using var verificationContext = factory.Create();
            var existing = await verificationContext.ExpenseCategories.FindAsync(42);
            Assert.AreEqual("CategoryNameChanged", existing.CategoryName);
            Assert.AreEqual("NameChanged", existing.Name);
            Assert.AreEqual(0, existing.BudgetedAmount);
            Assert.AreEqual(10, existing.BudgetedPercentage);
        }
    }
}

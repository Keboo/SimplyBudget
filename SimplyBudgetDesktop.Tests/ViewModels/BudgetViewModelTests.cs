using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
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
            using var _ = mocker.BeginDbScope();

            var vm = mocker.CreateInstance<BudgetViewModel>();
            var expenseCategory = await ExpenseCategoryViewModelEx.Create(mocker.Get<BudgetContext>(), new ExpenseCategory { ID = 42 });

            bool result = await vm.SaveChanges(expenseCategory);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task SaveChanges_WithFoundCategory_UpdatesBudgettedAmount()
        {
            AutoMocker mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.BeginDbScope();
            var context = mocker.Get<BudgetContext>();
            context.ExpenseCategories.Add(new ExpenseCategory
            {
                ID = 42,
                CategoryName = "CategoryName",
                Name = "Name",
                BudgetedAmount = 100
            });
            await context.SaveChangesAsync();

            var vm = mocker.CreateInstance<BudgetViewModel>();
            var expenseCategory = await ExpenseCategoryViewModelEx.Create(context, new ExpenseCategory { ID = 42 });
            expenseCategory.EditingCategory = "CategoryNameChanged";
            expenseCategory.EditingName = "NameChanged";
            expenseCategory.EditAmount = 120;
            expenseCategory.EditIsAmountType = true;

            bool result = await vm.SaveChanges(expenseCategory);

            Assert.IsTrue(result);
            var existing = await context.ExpenseCategories.FindAsync(42);
            Assert.AreEqual("CategoryNameChanged", existing.CategoryName);
            Assert.AreEqual("NameChanged", existing.Name);
            Assert.AreEqual(120, existing.BudgetedAmount);
            Assert.AreEqual(0, existing.BudgetedPercentage);
        }

        [TestMethod]
        public async Task SaveChanges_WithFoundCategory_UpdatesBudgettedPercentage()
        {
            AutoMocker mocker = new AutoMocker().WithDefaults();
            using var _ = mocker.BeginDbScope();
            var context = mocker.Get<BudgetContext>();
            context.ExpenseCategories.Add(new ExpenseCategory
            {
                ID = 42,
                CategoryName = "CategoryName",
                Name = "Name",
                BudgetedAmount = 100
            });
            await context.SaveChangesAsync();

            var vm = mocker.CreateInstance<BudgetViewModel>();
            var expenseCategory = await ExpenseCategoryViewModelEx.Create(context, new ExpenseCategory { ID = 42 });
            expenseCategory.EditingCategory = "CategoryNameChanged";
            expenseCategory.EditingName = "NameChanged";
            expenseCategory.EditAmount = 10;
            expenseCategory.EditIsAmountType = false;

            bool result = await vm.SaveChanges(expenseCategory);

            Assert.IsTrue(result);
            var existing = await context.ExpenseCategories.FindAsync(42);
            Assert.AreEqual("CategoryNameChanged", existing.CategoryName);
            Assert.AreEqual("NameChanged", existing.Name);
            Assert.AreEqual(0, existing.BudgetedAmount);
            Assert.AreEqual(10, existing.BudgetedPercentage);
        }
    }
}

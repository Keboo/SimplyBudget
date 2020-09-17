using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class IncomeTests
    {
        //[TestMethod]
        //public void IncomeDateRemovesOnlyPreservesDate()
        //{
        //    //Arrange
        //    var income = new Income();

        //    //Act
        //    income.Date = new DateTime(1, 1, 1, 1, 1, 1);

        //    //Assert
        //    Assert.AreEqual(new DateTime(1, 1, 1), income.Date);
        //}

        //[TestMethod]
        //public async Task TestGetIncomeItems()
        //{
        //    //Arrange
        //    var income = new Income();
        //    //var expectedItems = connection.MockQuery<IncomeItem>();

        //    //Act
        //    var items = await income.GetIncomeItems();

        //    //Assert
        //    //Assert.IsTrue(ReferenceEquals(expectedItems, items));
        //}

        //[TestMethod]
        //public async Task AddIncomeItemFailsForTransientIncome()
        //{
        //    //Arrange
        //    var income = new Income();

        //    //Act
        //    var incomeItem = await income.AddIncomeItem(1, 1);

        //    //Assert
        //    Assert.IsNull(incomeItem);
        //}

        //[TestMethod]
        //public async Task AddIncomeItemFailsForNoExpenseCategory()
        //{
        //    //Arrange
        //    var income = new Income();

        //    //Act
        //    var incomeItem = await income.AddIncomeItem(0, 1);

        //    //Assert
        //    Assert.IsNull(incomeItem);
        //}

        //[TestMethod]
        //public async Task TestAddIncomeItem()
        //{
        //    //Arrange
        //    const int AMOUNT = 100;
        //    const int EXPENSE_CATEGORY = 2;
        //    var income = new Income { ID = 1 };

        //    //Mock.Arrange(() => connection.InsertAsync(Arg.Matches<IncomeItem>(
        //    //    x => x.Amount == AMOUNT &&
        //    //         x.IncomeID == income.ID &&
        //    //         x.ExpenseCategoryID == EXPENSE_CATEGORY)))
        //    //         .Returns(Task.FromResult(0)).OccursOnce();
        //    //Mock.Arrange(() => connection.GetAsync<ExpenseCategory>(EXPENSE_CATEGORY))
        //    //    .Returns(Task.FromResult<ExpenseCategory>(null));

        //    //Act
        //    var incomeItem = await income.AddIncomeItem(EXPENSE_CATEGORY, AMOUNT);

        //    //Assert
        //    Assert.IsNotNull(incomeItem);
        //}
    }
}

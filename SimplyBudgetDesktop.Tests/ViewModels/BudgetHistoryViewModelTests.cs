using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudget.ViewModels;
using SimplyBudgetShared.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimplyBudgetDesktop.Tests.ViewModels
{
    [TestClass]
    public class BudgetHistoryViewModelTests
    {
        [TestMethod]
        public void Transactions_ShowCorrectDetails()
        {
            // Arrange
            var expenseCategory = new ExpenseCategory
            {
                ID = 72,
                Name = "Category Name"
            };
            var transaction = new ExpenseCategoryItem
            {
                ID = 42,
                Date = DateTime.Today,
                Description = "Transaction Description",
                Details = new List<ExpenseCategoryItemDetail>()
            };
            var detail = new ExpenseCategoryItemDetail
            {
                Amount = -500,
                ExpenseCategory = expenseCategory,
                ExpenseCategoryId = expenseCategory.ID,
                ExpenseCategoryItem = transaction,
                ExpenseCategoryItemId = transaction.ID,
                ID = 1
            };
            transaction.Details.Add(detail);

            //Act
            var vm = new BudgetHistoryViewModel(transaction, 200);

            //Assert
            Assert.AreEqual(transaction.Date, vm.Date);
            Assert.AreEqual(transaction.Description, vm.Description);
            Assert.AreEqual(200, vm.CurrentAmount);
            Assert.AreEqual($"({5.00:c})", vm.DisplayAmount);
            var item1 = vm.Details.Single();
            Assert.AreEqual($"({5.00:c})", item1.Amount);
            Assert.AreEqual(expenseCategory.Name, item1.ExpenseCategoryName);
        }

        [TestMethod]
        public void Income_ShowCorrectDetails()
        {
            // Arrange
            var expenseCategory = new ExpenseCategory
            {
                ID = 72,
                Name = "Category Name"
            };
            var income = new ExpenseCategoryItem
            {
                ID = 42,
                Date = DateTime.Today,
                Description = "Income Description",
                Details = new List<ExpenseCategoryItemDetail>()
            };
            var detail = new ExpenseCategoryItemDetail
            {
                Amount = 500,
                ExpenseCategory = expenseCategory,
                ExpenseCategoryId = expenseCategory.ID,
                ExpenseCategoryItem = income,
                ExpenseCategoryItemId = income.ID,
                ID = 1
            };
            income.Details.Add(detail);

            //Act
            var vm = new BudgetHistoryViewModel(income, 400);

            //Assert
            Assert.AreEqual(income.Date, vm.Date);
            Assert.AreEqual(income.Description, vm.Description);
            Assert.AreEqual(400, vm.CurrentAmount);
            Assert.AreEqual($"{5.00:c}", vm.DisplayAmount);
            var item1 = vm.Details.Single();
            Assert.AreEqual($"{5.00:c}", item1.Amount);
            Assert.AreEqual(expenseCategory.Name, item1.ExpenseCategoryName);
        }

        [TestMethod]
        public void Transfer_ShowCorrectDetails()
        {
            // Arrange
            var expenseCategoryFrom = new ExpenseCategory
            {
                ID = 72,
                Name = "From Category Name"
            };
            var expenseCategoryTo = new ExpenseCategory
            {
                ID = 73,
                Name = "To Category Name"
            };
            var transfer = new ExpenseCategoryItem
            {
                ID = 42,
                Date = DateTime.Today,
                Description = "Transfer Description",
                Details = new List<ExpenseCategoryItemDetail>()
            };
            var fromDetail = new ExpenseCategoryItemDetail
            {
                Amount = -450,
                ExpenseCategory = expenseCategoryFrom,
                ExpenseCategoryId = expenseCategoryFrom.ID,
                ExpenseCategoryItem = transfer,
                ExpenseCategoryItemId = transfer.ID,
                ID = 1
            };
            var toDetail = new ExpenseCategoryItemDetail
            {
                Amount = 450,
                ExpenseCategory = expenseCategoryTo,
                ExpenseCategoryId = expenseCategoryTo.ID,
                ExpenseCategoryItem = transfer,
                ExpenseCategoryItemId = transfer.ID,
                ID = 2
            };
            transfer.Details.Add(toDetail);
            transfer.Details.Add(fromDetail);

            //Act
            var vm = new BudgetHistoryViewModel(transfer, 400);

            //Assert
            Assert.AreEqual(transfer.Date, vm.Date);
            Assert.AreEqual(transfer.Description, vm.Description);
            Assert.AreEqual(400, vm.CurrentAmount);
            Assert.AreEqual($"<{4.50:c}>", vm.DisplayAmount);
            Assert.AreEqual(2, vm.Details.Count);

            var from = vm.Details[0];
            var to = vm.Details[1];

            Assert.AreEqual($"{-4.50:c}", from.Amount);
            Assert.AreEqual(expenseCategoryFrom.Name, from.ExpenseCategoryName);
            Assert.AreEqual($"{4.50:c}", to.Amount);
            Assert.AreEqual(expenseCategoryTo.Name, to.ExpenseCategoryName);
        }
    }
}

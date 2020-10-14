﻿using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Data
{
    [TestClass]
    public class IncomeTests
    {
        [TestMethod]
        public async Task OnRemove_RemovesChildren()
        {
            //Arrange
            var fixture = new BudgetDatabaseContext();

            Income? income = null;
            await fixture.PerformDatabaseOperation(async context =>
            {
                var category = new ExpenseCategory();
                context.Add(category);
                await context.SaveChangesAsync();
                income = await context.AddIncome("Test", DateTime.Now, (100, category.ID));
            });

            //Act
            await fixture.PerformDatabaseOperation(async context =>
            {
                income = await context.FindAsync<Income>(income!.ID);
                context.Remove(income);
                await context.SaveChangesAsync();
            });

            //Assert
            await fixture.PerformDatabaseOperation(async context =>
            {
                Assert.IsFalse(await context.Incomes.AnyAsync());
                Assert.IsFalse(await context.IncomeItems.AnyAsync());
            });
        }
    }
}

﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("ExpenseCategoryItemDetail")]
    public class ExpenseCategoryItemDetail : BaseItem, IBeforeCreate, IBeforeRemove
    {
        public int ExpenseCategoryItemId { get; set; }
        public ExpenseCategoryItem? ExpenseCategoryItem { get; set; }

        public int ExpenseCategoryId { get; set; }
        public ExpenseCategory? ExpenseCategory { get; set; }

        public int Amount { get; set; }

        public bool IgnoreBudget { get; set; }

        public async Task BeforeCreate(BudgetContext context)
        {
            var category = await context.FindAsync<ExpenseCategory>(ExpenseCategoryId);
            category.CurrentBalance += Amount;
        }

        public async Task BeforeRemove(BudgetContext context)
        {
            var category = await context.FindAsync<ExpenseCategory>(ExpenseCategoryId);
            category.CurrentBalance -= Amount;
        }

        public bool Equals([AllowNull] ExpenseCategoryItemDetail other)
        {
            return ExpenseCategoryItemId == other?.ExpenseCategoryItemId;
        }
    }
}
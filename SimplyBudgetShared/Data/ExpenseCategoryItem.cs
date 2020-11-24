using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    [Table("ExpenseCategoryItem")]
    public class ExpenseCategoryItem : BaseItem, IBeforeRemove
    {
        private DateTime _date;
        public DateTime Date
        {
            get => _date;
            set => _date = value.Date;  //Ensure that we only capture the date
        }

        public string? Description { get; set; }

        public List<ExpenseCategoryItemDetail>? Details { get; set; }

        public async Task BeforeRemove(BudgetContext context)
        {
            await foreach (var item in context.ExpenseCategoryItemDetails.Where(x => x.ExpenseCategoryItemId == ID).AsAsyncEnumerable())
            {
                context.Remove(item);
            }
        }

        public bool IsTransfer =>
            Details?.Count == 2 &&
            Details[0].Amount + Details[1].Amount == 0;

        public static implicit operator Transaction(ExpenseCategoryItem item)
        {
            var transaction =  new Transaction
            {
                Date = item.Date,
                Description = item.Description,
                ID = item.ID
            };

            transaction.TransactionItems = item.Details?.Select(x => new TransactionItem
            {
                Amount = -x.Amount,
                Description = item.Description,
                ExpenseCategory = x.ExpenseCategory,
                ExpenseCategoryID = x.ExpenseCategoryId,
                ID = x.ID,
                Transaction = transaction,
                TransactionID = transaction.ID
            }).ToList();
            return transaction;
        }

        public static implicit operator Income(ExpenseCategoryItem item)
        {
            var income = new Income
            {
                Date = item.Date,
                Description = item.Description,
                ID = item.ID,
            };

            income.IncomeItems = item.Details?.Select(x => new IncomeItem
            {
                Amount = -x.Amount,
                Description = item.Description,
                ExpenseCategory = x.ExpenseCategory,
                ExpenseCategoryID = x.ExpenseCategoryId,
                ID = x.ID,
                Income = income,
                IncomeID = income.ID
            }).ToList();
            
            income.TotalAmount = income.IncomeItems.Sum(x => x.Amount);

            return income;
        }

        public static implicit operator Transfer(ExpenseCategoryItem item)
        {
            if (item.Details?.Count != 2) throw new InvalidOperationException("Item must have exactly two items to be a transfer");
            if (item.Details[0].Amount != -item.Details[1].Amount)
            {
                throw new InvalidOperationException("Amounts must match to be converted to a transfer");
            }

            var transfer = new Transfer
            {
                Date = item.Date,
                Description = item.Description,
                ID = item.ID,
                Amount = Math.Abs(item.Details[0].Amount),
                FromExpenseCategoryID = item.Details.First(x => x.Amount <= 0).ExpenseCategoryId,
                FromExpenseCategory = item.Details.First(x => x.Amount <= 0).ExpenseCategory,
                ToExpenseCategoryID = item.Details.Last(x => x.Amount >= 0).ExpenseCategoryId,
                ToExpenseCategory = item.Details.Last(x => x.Amount >= 0).ExpenseCategory
            };
            return transfer;
        }
    }
}
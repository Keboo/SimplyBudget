using System.Diagnostics.CodeAnalysis;

namespace SimplyBudget.Data;

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
        var category = await context.FindAsync<ExpenseCategory>(ExpenseCategory?.ID ?? ExpenseCategoryId)
            ?? throw new InvalidOperationException("Could not find expense category for item");
        category.CurrentBalance += Amount;
    }

    public async Task BeforeRemove(BudgetContext context)
    {
        if (await context.FindAsync<ExpenseCategory>(ExpenseCategoryId) is { } category)
        {
            category.CurrentBalance -= Amount;
        }
    }

    public bool Equals([AllowNull] ExpenseCategoryItemDetail other)
    {
        return ExpenseCategoryItemId == other?.ExpenseCategoryItemId;
    }
}
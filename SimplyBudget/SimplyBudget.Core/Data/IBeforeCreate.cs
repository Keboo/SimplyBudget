namespace SimplyBudget.Core.Data;

internal interface IBeforeCreate
{
    Task BeforeCreate(BudgetContext context);
}

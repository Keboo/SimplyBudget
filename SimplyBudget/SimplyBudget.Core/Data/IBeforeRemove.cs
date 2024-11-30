namespace SimplyBudget.Core.Data;

internal interface IBeforeRemove
{
    Task BeforeRemove(BudgetContext context);
}

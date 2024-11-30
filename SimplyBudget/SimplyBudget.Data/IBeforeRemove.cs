namespace SimplyBudget.Data;

internal interface IBeforeRemove
{
    Task BeforeRemove(BudgetContext context);
}

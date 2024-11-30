namespace SimplyBudget.Data;

internal interface IBeforeCreate
{
    Task BeforeCreate(BudgetContext context);
}

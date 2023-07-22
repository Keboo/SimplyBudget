namespace SimplyBudget;

public interface IDispatcher
{
    Task InvokeAsync(Action callback);
}

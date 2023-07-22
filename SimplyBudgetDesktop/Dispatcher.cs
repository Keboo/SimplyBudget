namespace SimplyBudget;

public class Dispatcher : IDispatcher
{
    public async Task InvokeAsync(Action callback) => await System.Windows.Application.Current.Dispatcher.InvokeAsync(callback);
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Threading;

public static class TaskEx
{
    private static AsyncLocal<TaskScheduler?> _default = new();
    public static TaskScheduler? Default
    {
        get => _default.Value;
        set => _default.Value = value;
    }

    public static Task Run(Action action)
    {
        var task = new Task(action);
        task.Start(Default ?? TaskScheduler.Current);
        return task;
    }
}

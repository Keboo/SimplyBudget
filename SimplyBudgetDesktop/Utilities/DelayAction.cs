using System;
using System.Windows.Threading;

namespace SimplyBudget.Utilities
{
    public class DelayAction
    {
        public event EventHandler<EventArgs>? Action;

        private readonly DispatcherTimer _timer;
        public DelayAction(TimeSpan? delay = null)
        {
            _timer = new DispatcherTimer {Interval = delay ?? TimeSpan.FromMilliseconds(300)};
            _timer.Tick += TimerOnTick;
        }

        private void TimerOnTick(object? sender, EventArgs eventArgs)
        {
            _timer.Stop();
            Action?.Invoke(this, EventArgs.Empty);
        }

        public void RaiseAction()
        {
            _timer.Stop();
            _timer.Start();
        }
    }
}
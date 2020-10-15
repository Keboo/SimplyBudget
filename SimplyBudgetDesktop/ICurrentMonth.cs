using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Utilities;
using System;

namespace SimplyBudget
{
    public interface ICurrentMonth
    {
        DateTime CurrenMonth { get; set; }
    }

    public class CurrentMonth : ICurrentMonth
    {
        private DateTime _currentMonth = DateTime.Today.StartOfMonth();
        public DateTime CurrenMonth 
        {
            get => _currentMonth;
            set
            {
                value = value.StartOfMonth();
                if (value != _currentMonth)
                {
                    _currentMonth = value;
                    Messenger.Send(new CurrentMonthChanged(value));
                }
            }
        }
        private IMessenger Messenger { get; }

        public CurrentMonth(IMessenger messenger)
        {
            if (messenger is null)
            {
                throw new ArgumentNullException(nameof(messenger));
            }
            Messenger = messenger;
        }
    }
}

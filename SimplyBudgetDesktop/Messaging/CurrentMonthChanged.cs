using System;

namespace SimplyBudget.Messaging
{
    public class CurrentMonthChanged
    {
        public CurrentMonthChanged(DateTime startOfMonth)
        {
            StartOfMonth = startOfMonth;
        }

        public DateTime StartOfMonth { get; }
    }
}

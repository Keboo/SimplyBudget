using System;

namespace SimplyBudget.Messaging
{
    public record CurrentMonthChanged(DateTime StartOfMonth)
    {
    }
}

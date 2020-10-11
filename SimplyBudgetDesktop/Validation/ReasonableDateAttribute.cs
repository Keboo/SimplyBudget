using SimplyBudgetShared.Utilities;
using System;
using System.ComponentModel.DataAnnotations;

namespace SimplyBudget.Validation
{
    public class ReasonableDateAttribute : ValidationAttribute
    {
        private const int MaxMonthsInThePast = 2;
        private const int MaxMonthsInTheFuture = 1;

        public override bool IsValid(object value)
        {
            if (value is DateTime dateTime)
            {
                if (Start > dateTime)
                {
                    return false;
                }
                if (End < dateTime)
                {
                    return false;
                }
            }
            return true;
        }

        private static DateTime Start => DateTime.Now.AddMonths(-MaxMonthsInThePast).StartOfMonth();

        private static DateTime End => DateTime.Now.AddMonths(MaxMonthsInTheFuture).EndOfMonth();

        public override string FormatErrorMessage(string name) => $"{name} should be between {Start:d} and {End:d}";
    }
}

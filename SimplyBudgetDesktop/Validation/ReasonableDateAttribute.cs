using SimplyBudgetShared.Utilities;
using System.ComponentModel.DataAnnotations;

namespace SimplyBudget.Validation;

public class ReasonableDateAttribute : ValidationAttribute
{
    private const int MaxMonthsInThePast = 2;
    private const int MaxMonthsInTheFuture = 1;

    public ReasonableDateAttribute()
        : this(null!)
    {
    }

    public ReasonableDateAttribute(ICurrentMonth currentMonth)
    {
        CurrentMonth = currentMonth ?? DI.GetService<ICurrentMonth>() 
            ?? throw new ArgumentNullException(nameof(currentMonth));
    }

    public override bool IsValid(object? value)
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

    private DateTime Start => CurrentMonth.CurrenMonth.AddMonths(-MaxMonthsInThePast).StartOfMonth();

    private DateTime End => CurrentMonth.CurrenMonth.AddMonths(MaxMonthsInTheFuture).EndOfMonth();

    private ICurrentMonth CurrentMonth { get; }

    public override string FormatErrorMessage(string name) => $"{name} should be between {Start:d} and {End:d}";
}

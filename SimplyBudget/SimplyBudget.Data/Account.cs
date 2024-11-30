﻿namespace SimplyBudget.Data;

public class Account : BaseItem
{
    public string? Name { get; set; }

    private DateTime _validatedDate;
    public DateTime ValidatedDate
    {
        get => _validatedDate;
        set => _validatedDate = value.Date;  //Ensure we only capture the date
    }

    public bool IsDefault { get; internal set; }

    public List<ExpenseCategory>? ExpenseCategories { get; set; }
}
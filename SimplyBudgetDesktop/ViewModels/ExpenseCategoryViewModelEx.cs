using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels;

public class ExpenseCategoryViewModelEx : ExpenseCategoryViewModel
{
    public static async Task<ExpenseCategoryViewModelEx> Create(IDataClient dataClient, ExpenseCategory expenseCategory)
    {
        return await Create(dataClient, expenseCategory, DateTime.Today);
    }

    public static async Task<ExpenseCategoryViewModelEx> Create(IDataClient dataClient, ExpenseCategory expenseCategory, DateTime month)
    {
        if (expenseCategory is null) throw new ArgumentNullException(nameof(expenseCategory));

        var categoryItems = await dataClient.GetCategoryItemDetailsAsync(expenseCategory, month.StartOfMonth(), month.EndOfMonth());

        var accounts = await dataClient.Accounts.GetAllAsync().ToListAsync();

        var rv = new ExpenseCategoryViewModelEx(expenseCategory.ID)
        {
            Accounts = accounts,
            Account = expenseCategory.Account ?? accounts.FirstOrDefault(x => x.ID == expenseCategory.AccountID)
        };
        SetProperties(expenseCategory, rv);
        rv.MonthlyExpenses = categoryItems
            .Where(x => x.IgnoreBudget == false)
            .Where(x => x.Amount < 0 && x.ExpenseCategoryItem?.IsTransfer == false)
            .Sum(x => -x.Amount);

        rv.MonthlyAllocations = categoryItems
            .Where(x => x.IgnoreBudget == false)
            .Where(x => x.Amount > 0 || x.ExpenseCategoryItem?.IsTransfer == true)
            .Sum(x => x.Amount);
        rv.BudgetedAmountDisplay = expenseCategory.GetBudgetedDisplayString();

        rv.ThreeMonthAverage = await GetAverage(dataClient, expenseCategory, month.AddMonths(-3).StartOfMonth(), month.EndOfMonth());
        rv.SixMonthAverage = await GetAverage(dataClient, expenseCategory, month.AddMonths(-6).StartOfMonth(), month.EndOfMonth());
        rv.TwelveMonthAverage = await GetAverage(dataClient, expenseCategory, month.AddMonths(-12).StartOfMonth(), month.EndOfMonth());
        return rv;

        static async Task<int> GetAverage(IDataClient dataClient, ExpenseCategory expenseCategory, DateTime start, DateTime end)
        {
            var categoryItems = await dataClient.GetCategoryItemDetailsAsync(expenseCategory, start, end);
            var items = categoryItems
                        .Where(x => x.IgnoreBudget == false && x.Amount < 0)
                        .GroupBy(x => x.ExpenseCategoryItem?.Date.StartOfMonth())
                        .Select(g => g.Select(x => -x.Amount).Sum())
                        .ToList();
            return items.Count != 0 ? (int)items.Average() : 0;
        }
    }

    public static async Task<ExpenseCategoryViewModelEx> Create(Func<BudgetContext> contextFactory, ExpenseCategory expenseCategory)
    {
        return await Create(contextFactory, expenseCategory, DateTime.Today);
    }

    public static async Task<ExpenseCategoryViewModelEx> Create(Func<BudgetContext> contextFactory, ExpenseCategory expenseCategory, DateTime month)
    {
        if (contextFactory is null)
        {
            throw new ArgumentNullException(nameof(contextFactory));
        }

        if (expenseCategory is null) throw new ArgumentNullException(nameof(expenseCategory));

        using var context = contextFactory();
        var categoryItems = await context.GetCategoryItemDetails(expenseCategory, month.StartOfMonth(), month.EndOfMonth());

        var rv = new ExpenseCategoryViewModelEx(expenseCategory.ID)
        {
            Accounts = await context.Accounts.ToListAsync(),
            Account = expenseCategory.Account ?? await context.Accounts.FirstOrDefaultAsync(x => x.ID == expenseCategory.AccountID)
        };
        SetProperties(expenseCategory, rv);
        rv.MonthlyExpenses = categoryItems
            .Where(x => x.IgnoreBudget == false)
            .Where(x => x.Amount < 0 && x.ExpenseCategoryItem?.IsTransfer == false)
            .Sum(x => -x.Amount);

        rv.MonthlyAllocations = categoryItems
            .Where(x => x.IgnoreBudget == false)
            .Where(x => x.Amount > 0 || x.ExpenseCategoryItem?.IsTransfer == true)
            .Sum(x => x.Amount);
        rv.BudgetedAmountDisplay = expenseCategory.GetBudgetedDisplayString();

        rv.ThreeMonthAverage = await GetAverage(context, expenseCategory, month.AddMonths(-3).StartOfMonth(), month.EndOfMonth());
        rv.SixMonthAverage = await GetAverage(context, expenseCategory, month.AddMonths(-6).StartOfMonth(), month.EndOfMonth());
        rv.TwelveMonthAverage = await GetAverage(context, expenseCategory, month.AddMonths(-12).StartOfMonth(), month.EndOfMonth());
        return rv;

        static async Task<int> GetAverage(BudgetContext context, ExpenseCategory expenseCategory, DateTime start, DateTime end)
        {
            var categoryItems = await context.GetCategoryItemDetails(expenseCategory, start, end);
            var items = categoryItems
                        .Where(x => x.IgnoreBudget == false && x.Amount < 0)
                        .GroupBy(x => x.ExpenseCategoryItem?.Date.StartOfMonth())
                        .Select(g => g.Select(x => -x.Amount).Sum())
                        .ToList();
            return items.Count != 0 ? (int)items.Average() : 0;
        }
    }

    private ExpenseCategoryViewModelEx(int expenseCategoryID)
        : base(expenseCategoryID)
    { }

    public IList<Account>? Accounts { get; init; }

    private Account? _account;
    public Account? Account
    {
        get => _account;
        set => SetProperty(ref _account, value);
    }

    private int _monthlyExpenses;
    public int MonthlyExpenses
    {
        get => _monthlyExpenses;
        set => SetProperty(ref _monthlyExpenses, value);
    }

    private int _monthlyAllocations;
    public int MonthlyAllocations
    {
        get => _monthlyAllocations;
        set => SetProperty(ref _monthlyAllocations, value);
    }

    private string? _budgetedAmountDisplay;
    public string? BudgetedAmountDisplay
    {
        get => _budgetedAmountDisplay;
        set => SetProperty(ref _budgetedAmountDisplay, value);
    }

    private int _threeMonthAverage;
    public int ThreeMonthAverage
    {
        get => _threeMonthAverage;
        set => SetProperty(ref _threeMonthAverage, value);
    }

    private int _sixMonthAverage;
    public int SixMonthAverage
    {
        get => _sixMonthAverage;
        set => SetProperty(ref _sixMonthAverage, value);
    }

    private int _twelveMonthAverage;
    public int TwelveMonthAverage
    {
        get => _twelveMonthAverage;
        set => SetProperty(ref _twelveMonthAverage, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value) && value)
            {
                EditingName = Name;
                EditingCategory = CategoryName;
                EditIsAmountType = BudgetedPercentage <= 0;
                EditAmount = BudgetedPercentage > 0 ? BudgetedPercentage : BudgetedAmount;
                EditingCap = Cap;
            }
        }
    }

    private string? _editingName;
    public string? EditingName
    {
        get => _editingName;
        set => SetProperty(ref _editingName, value);
    }

    private string? _editingCategory;
    public string? EditingCategory
    {
        get => _editingCategory;
        set => SetProperty(ref _editingCategory, value);
    }

    private bool _editIsAmountType;
    public bool EditIsAmountType
    {
        get => _editIsAmountType;
        set => SetProperty(ref _editIsAmountType, value);
    }

    private int _editAmount;
    public int EditAmount
    {
        get => _editAmount;
        set => SetProperty(ref _editAmount, value);
    }

    private int? _editingCap;
    public int? EditingCap
    {
        get => _editingCap;
        set => SetProperty(ref _editingCap, value);
    }
}
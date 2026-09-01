namespace SimplyBudgetShared.DataTransfer;

public sealed class BudgetDataExportPackage
{
    public const int CurrentFormatVersion = 2;

    public int FormatVersion { get; set; } = CurrentFormatVersion;

    public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Source { get; set; }

    public List<BudgetDataExportAccount> Accounts { get; set; } = [];

    public List<BudgetDataExportCategory> Categories { get; set; } = [];

    public List<BudgetDataExportItem> Items { get; set; } = [];

    public List<BudgetDataExportItemDetail> ItemDetails { get; set; } = [];

    public List<BudgetDataExportRule> Rules { get; set; } = [];

    public List<BudgetDataExportMetadata> Metadata { get; set; } = [];
}

public record BudgetDataExportAccount(
    int Id,
    string? Name,
    DateTime ValidatedDate,
    bool IsDefault
);

public record BudgetDataExportCategory(
    int Id,
    string? Name,
    string? Description,
    string? CategoryName,
    int? AccountId,
    int BudgetedAmount,
    int BudgetedPercentage,
    int CurrentBalance,
    int? Cap,
    bool IsHidden
);

public record BudgetDataExportItem(
    int Id,
    DateTime Date,
    string? Description,
    string? Notes = null
);

public record BudgetDataExportItemDetail(
    int Id,
    int ExpenseCategoryItemId,
    int ExpenseCategoryId,
    int Amount,
    bool IgnoreBudget
);

public record BudgetDataExportRule(
    int Id,
    string? Name,
    string? RuleRegex,
    int? ExpenseCategoryId,
    string? Notes = null
);

public record BudgetDataExportMetadata(
    int Id,
    string? Key,
    string? Value
);

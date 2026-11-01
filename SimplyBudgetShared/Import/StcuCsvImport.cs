using CsvHelper;

using SimplyBudgetShared.Data;

namespace SimplyBudgetShared.Import;

public class StcuCsvImport : CsvImport<StcuRecord>
{
    public StcuCsvImport(Stream csvData)
        : base(csvData)
    { }

    protected override ExpenseCategoryItem? ConvertRow(StcuRecord row)
    {
        if (row.Amount is null) return null;
        if (IsTransfer(row)) return null;

        switch (row.TransactionType?.ToLowerInvariant())
        {
            case "check":
            case "debit":
                return new()
                {
                    Date = row.EffectiveDate?.Date ?? row.PostingDate?.Date ?? DateTime.Today,
                    Description = row.Description,
                    Details =
                    [
                        new()
                        {
                            Amount = (int)(row.Amount * 100),
                        }
                    ]
                };
            case "credit":
                return new()
                {
                    Date = row.EffectiveDate?.Date ?? row.PostingDate?.Date ?? DateTime.Today,
                    Description = row.Description,
                    Details =
                    [
                        new()
                        {
                            Amount = (int)(row.Amount * 100),
                        }
                    ]
                };
            default:
                throw new InvalidOperationException($"Unknown transaction type '{row.TransactionType}'");
        }
    }

    private static bool IsTransfer(StcuRecord row)
        => string.Equals(row.Type?.Trim(), "Transfer", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Description?.Trim(), "efund transfer", StringComparison.OrdinalIgnoreCase);
}

[CsvRecord]
public partial class StcuRecord
{
    [CsvColumn("Posting Date")]
    public DateTime? PostingDate { get; set; }

    [CsvColumn("Effective Date")]
    public DateTime? EffectiveDate { get; set; }

    [CsvColumn("Transaction Type")]
    public string? TransactionType { get; set; }

    [CsvColumn("Amount")]
    public decimal? Amount { get; set; }

    [CsvColumn("Check Number")]
    public string? CheckNumber { get; set; }

    [CsvColumn("Description")]
    public string? Description { get; set; }

    [CsvColumn("Transaction Category")]
    public string? TransactionCategory { get; set; }

    [CsvColumn("Type")]
    public string? Type { get; set; }
}
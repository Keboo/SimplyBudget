using CsvHelper.Configuration.Attributes;
using SimplyBudgetShared.Data;
using System;
using System.IO;

namespace SimplyBudgetShared.Import
{
    public class StcuCsvImport : CsvImport<StcuRecord>
    {
        public StcuCsvImport(Stream csvData)
            : base(csvData)
        { }

        protected override ExpenseCategoryItem? ConvertRow(StcuRecord row)
        {
            if (row.Amount is null) return null;
            switch (row.TransactionType?.ToLowerInvariant())
            {
                case "check":
                case "debit" when !string.Equals(row.Type, "Transfer", StringComparison.Ordinal):
                    return new()
                    {
                        Date = row.EffectiveDate?.Date ?? DateTime.Today,
                        Description = row.Description,
                        Details = new()
                        {
                            new()
                            {
                                Amount = (int)(row.Amount * 100),
                            }
                        }
                    };
                case "debit": return null;
                case "credit":
                    return new()
                    {
                        Date = row.EffectiveDate?.Date ?? DateTime.Today,
                        Description = row.Description,
                        Details = new()
                        {
                            new()
                            {
                                Amount = (int)(row.Amount * 100),
                            }
                        }
                    };
                default:
                    throw new InvalidOperationException($"Unknown transaction type '{row.TransactionType}'");
            }
        }
    }

    public class StcuRecord
    {
        [Name("Effective Date")]
        public DateTime? EffectiveDate { get; set; }

        [Name("Transaction Type")]
        public string? TransactionType { get; set; }

        [Name("Amount")]
        public decimal? Amount { get; set; }

        [Name("Check Number")]
        public string? CheckNumber { get; set; }

        [Name("Description")]
        public string? Description { get; set; }

        [Name("Transaction Category")]
        public string? TransactionCategory { get; set; }

        [Name("Type")]
        public string? Type { get; set; }
    }
}

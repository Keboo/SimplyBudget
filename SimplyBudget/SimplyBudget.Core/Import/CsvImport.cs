using System.Globalization;

using CsvHelper;

using SimplyBudget.Core.Data;

namespace SimplyBudget.Core.Import;

public abstract class CsvImport<TRow> : IImport
    where TRow : new()
{
    public CsvImport(Stream csvData)
    {
        CsvData = csvData ?? throw new ArgumentNullException(nameof(csvData));
    }

    private Stream CsvData { get; }

    public async IAsyncEnumerable<ExpenseCategoryItem> GetItems()
    {
        using var reader = new StreamReader(CsvData);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        TRow item = new();
        await foreach (var record in csv.EnumerateRecordsAsync(item))
        {
            if (ConvertRow(record) is { } converted)
            {
                yield return converted;
            }
        }
    }

    protected abstract ExpenseCategoryItem? ConvertRow(TRow row);
}

using CsvHelper;
using SimplyBudgetShared.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SimplyBudgetShared.Import
{
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
            using (var reader = new StreamReader(CsvData))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                TRow item = new();
                await foreach(var record in csv.EnumerateRecordsAsync(item))
                {
                    if (ConvertRow(record) is { } converted)
                    {
                        yield return converted;
                    }
                }
            }
        }

        protected abstract ExpenseCategoryItem? ConvertRow(TRow row);
    }
}

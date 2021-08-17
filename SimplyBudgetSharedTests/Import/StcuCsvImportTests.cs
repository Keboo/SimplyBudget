using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Import;
using SimplyBudgetShared.Utilities;
using System.Text;

namespace SimplyBudgetSharedTests.Import
{
    [TestClass]
    public class StcuCsvImportTests
    {
        [TestMethod]
        public async Task CanRead_Transactions()
        {
            string csv = @"""2020112312345678,12345,123,123,123,123,123"",""11/23/2020"",""11/23/2020"",""Debit"",""-21.77000"","""",""500000000"",""Purchase GOOGLE Play"",""Entertainment"",""Debit Card"",""10000.00000"","""",""Purchase GOOGLE Play""
""2020112212345678,123456,123,123,123,123,123"",""11/22/2020"",""11/23/2020"",""Debit"",""-23.48000"","""",""500000000"",""Purchase MCDONALD'S SPOKANE      WAUS"",""Food & Dining"",""Debit Card"",""10000.00000"","""",""Purchase MCDONALD'S SPOKANE      WAUS""";
            using var stream = AsStream(csv);
            var csvImport = new StcuCsvImport(stream);

            var items = await csvImport.GetItems().ToListAsync();

            Assert.AreEqual(2, items.Count);

            Assert.AreEqual(new DateTime(2020, 11, 23), items[0].Date);
            Assert.AreEqual("Purchase GOOGLE Play", items[0].Description);
            Assert.AreEqual(1, items[0].Details?.Count);
            Assert.AreEqual(-2177, items[0].Details?[0].Amount);

            Assert.AreEqual(new DateTime(2020, 11, 22), items[1].Date);
            Assert.AreEqual("Purchase MCDONALD'S SPOKANE      WAUS", items[1].Description);
            Assert.AreEqual(1, items[1].Details?.Count);
            Assert.AreEqual(-2348, items[1].Details?[0].Amount);
        }

        [TestMethod]
        public async Task CanRead_Checks()
        {
            string csv = @"""2020111912345678,12345,123,123,123,123,123"",""11/19/2020"",""11/19/2020"",""Check"",""-24.00000"",""1471"",""500000000"",""Check #1471"","""",""Check"",""10000.00000"","""",""Check #1471""";
            using var stream = AsStream(csv);
            var csvImport = new StcuCsvImport(stream);

            var items = await csvImport.GetItems().ToListAsync();

            Assert.AreEqual(1, items.Count);

            Assert.AreEqual(new DateTime(2020, 11, 19), items[0].Date);
            Assert.AreEqual("Check #1471", items[0].Description);
            Assert.AreEqual(1, items[0].Details?.Count);
            Assert.AreEqual(-2400, items[0].Details?[0].Amount);
        }

        [TestMethod]
        public async Task CanRead_Credits()
        {
            string csv = @"""2020112012345678,12345,123,123,123,123,123"",""11/20/2020"",""11/20/2020"",""Credit"",""1234.56000"","""",""500000000"",""PAYROLL"","""",""ACH"",""10000.00000"","""",""PAYROLL""";
            using var stream = AsStream(csv);
            var csvImport = new StcuCsvImport(stream);

            var items = await csvImport.GetItems().ToListAsync();

            Assert.AreEqual(1, items.Count);

            Assert.AreEqual(new DateTime(2020, 11, 20), items[0].Date);
            Assert.AreEqual("PAYROLL", items[0].Description);
            Assert.AreEqual(1, items[0].Details?.Count);
            Assert.AreEqual(123456, items[0].Details?[0].Amount);
        }

        [TestMethod]
        public async Task CanRead_Transactions_IgnoresTransfers()
        {
            string csv = @"""2020112212345678,12345,123,123,123,123,123"",""11/22/2020"",""11/22/2020"",""Debit"",""-50.00000"","""",""500000000"",""Online Banking Transfer"","""",""Transfer"",""10000.00000"","""",""Online Banking Transfer""";
            using var stream = AsStream(csv);
            var csvImport = new StcuCsvImport(stream);

            var items = await csvImport.GetItems().ToListAsync();

            Assert.AreEqual(0, items.Count);
        }

        [TestMethod]
        [Description("Issue 3")]
        public async Task Import_WithDifferentEffectiveAndPostingDate_UsesPostingDate()
        {
            string csv = @"""2021013112784791,75120,210,201,446,006,044"",""1/31/2021"",""2/1/2021"",""Debit"",""-17.51000"","""",""531221069"",""Purchase MCDONALD'S F570,6321 N MONROE          SPOKANE      WAUS"",""Food & Dining"",""Debit Card"",""40653.62000"","""",""Purchase MCDONALD'S F570,6321 N MONROE          SPOKANE      WAUS""";
            using var stream = AsStream(csv);
            var csvImport = new StcuCsvImport(stream);

            var items = await csvImport.GetItems().ToListAsync();

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(new DateTime(2021, 1, 31), items[0].Date);
        }

        private const string StcuHeader = @"""Transaction ID"",""Posting Date"",""Effective Date"",""Transaction Type"",""Amount"",""Check Number"",""Reference Number"",""Description"",""Transaction Category"",""Type"",""Balance"",""Memo"",""Extended Description""";

        private static Stream AsStream(string csvData)
            => new MemoryStream(Encoding.UTF8.GetBytes(StcuHeader + Environment.NewLine + csvData));
    }
}

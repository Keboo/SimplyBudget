using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Import;
using SimplyBudgetShared.Utilities;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SimplyBudgetSharedTests.Import
{
    [TestClass]
    public class StcuCsvImportTests
    {
        [TestMethod]
        public async Task CanRead_Transactions()
        {
            string csv = @"""2020112312345678,12345,123,123,123,123,123"",""11/23/2020"",""11/23/2020"",""Debit"",""-21.77000"","""",""500000000"",""Purchase GOOGLE Play"",""Entertainment"",""Debit Card"",""26877.27000"","""",""Purchase GOOGLE Play""
""2020112212345678,123456,123,123,123,123,123"",""11/22/2020"",""11/23/2020"",""Debit"",""-23.48000"","""",""512799927"",""Purchase MCDONALD'S SPOKANE      WAUS"",""Food & Dining"",""Debit Card"",""27934.51000"","""",""Purchase MCDONALD'S SPOKANE      WAUS""";
            using var stream = AsStream(csv);
            var csvImport = new StcuCsvImport(stream);

            var items = await csvImport.GetItems().ToListAsync();

            Assert.AreEqual(2, items.Count);

            Assert.AreEqual(new DateTime(2020, 11, 23), items[0].Date);
            Assert.AreEqual("Purchase GOOGLE Play", items[0].Description);
            Assert.AreEqual(1, items[0].Details?.Count);
            Assert.AreEqual(-2177, items[0].Details?[0].Amount);

            Assert.AreEqual(new DateTime(2020, 11, 23), items[1].Date);
            Assert.AreEqual("Purchase MCDONALD'S SPOKANE      WAUS", items[1].Description);
            Assert.AreEqual(1, items[1].Details?.Count);
            Assert.AreEqual(-2348, items[1].Details?[0].Amount);
        }

        private const string StcuHeader = @"""Transaction ID"",""Posting Date"",""Effective Date"",""Transaction Type"",""Amount"",""Check Number"",""Reference Number"",""Description"",""Transaction Category"",""Type"",""Balance"",""Memo"",""Extended Description""";

        private static Stream AsStream(string csvData)
            => new MemoryStream(Encoding.UTF8.GetBytes(StcuHeader + Environment.NewLine + csvData));
    }
}

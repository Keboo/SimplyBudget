using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq.AutoMock;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.DataTransfer;

namespace SimplyBudgetSharedTests.Data;

[TestClass]
public class BudgetDataPortabilityServiceTests
{
    [TestMethod]
    public async Task ExportAsync_IncludesAllBudgetData()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        var checking = new Account { Name = "Checking", ValidatedDate = new DateTime(2026, 1, 1) };
        var savings = new Account { Name = "Savings", ValidatedDate = new DateTime(2026, 1, 2) };

        using var setupContext = factory.Create();
        setupContext.Accounts.AddRange(checking, savings);
        await setupContext.SaveChangesAsync();
        savings = await setupContext.SetAsDefaultAsync(savings);
        await setupContext.SaveChangesAsync();

        var groceries = new ExpenseCategory
        {
            Name = "Groceries",
            CategoryName = "Food",
            AccountID = checking.ID,
            BudgetedAmount = 500_00
        };
        setupContext.ExpenseCategories.Add(groceries);
        await setupContext.SaveChangesAsync();

        var paycheck = await setupContext.AddIncome("Paycheck", new DateTime(2026, 2, 1), (1_500_00, groceries.ID));
        paycheck.Notes = "February salary";
        setupContext.ExpenseCategoryRules.Add(new ExpenseCategoryRule
        {
            Name = "Grocery Rule",
            RuleRegex = "GROCERY",
            ExpenseCategoryID = groceries.ID
        });
        setupContext.Metadatas.Add(new Metadata { Key = "Version", Value = "test" });
        await setupContext.SaveChangesAsync();

        // Act
        BudgetDataExportPackage exportPackage = await BudgetDataPortabilityService.ExportAsync(
            setupContext,
            source: "desktop-test");

        // Assert
        Assert.AreEqual(BudgetDataExportPackage.CurrentFormatVersion, exportPackage.FormatVersion);
        Assert.AreEqual("desktop-test", exportPackage.Source);
        Assert.AreEqual(2, exportPackage.Accounts.Count);
        Assert.AreEqual(1, exportPackage.Categories.Count);
        Assert.AreEqual(1, exportPackage.Items.Count);
        Assert.AreEqual(1, exportPackage.ItemDetails.Count);
        Assert.AreEqual(1, exportPackage.Rules.Count);
        Assert.AreEqual(1, exportPackage.Metadata.Count);
        Assert.IsTrue(exportPackage.Accounts.Single(x => x.Name == "Savings").IsDefault);
        Assert.AreEqual("February salary", exportPackage.Items.Single().Notes);
    }

    [TestMethod]
    public async Task ImportAsync_ReplacesExistingDataAndPreservesRelationships()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        using (var seedContext = factory.Create())
        {
            seedContext.Accounts.Add(new Account { Name = "Old Account", ValidatedDate = DateTime.Today });
            await seedContext.SaveChangesAsync();
        }

        var exportPackage = new BudgetDataExportPackage
        {
            Source = "desktop",
            Accounts =
            [
                new BudgetDataExportAccount(10, "Checking", new DateTime(2026, 3, 1), false),
                new BudgetDataExportAccount(20, "Savings", new DateTime(2026, 3, 2), true)
            ],
            Categories =
            [
                new BudgetDataExportCategory(100, "Groceries", "Food", 10, 500_00, 0, 200_00, null, false),
                new BudgetDataExportCategory(200, "Emergency Fund", "Savings", 20, 0, 10, 300_00, null, false)
            ],
            Items =
            [
                new BudgetDataExportItem(1000, new DateTime(2026, 3, 5), "Paycheck", "March salary"),
                new BudgetDataExportItem(2000, new DateTime(2026, 3, 7), "Market")
            ],
            ItemDetails =
            [
                new BudgetDataExportItemDetail(5000, 1000, 100, 2_000_00, false),
                new BudgetDataExportItemDetail(6000, 1000, 200, 1_000_00, false),
                new BudgetDataExportItemDetail(7000, 2000, 100, -500_00, false)
            ],
            Rules =
            [
                new BudgetDataExportRule(9000, "Market Rule", "MARKET", 100)
            ],
            Metadata =
            [
                new BudgetDataExportMetadata(9100, "Version", "1")
            ]
        };

        // Act
        using (var importContext = factory.Create())
        {
            await BudgetDataPortabilityService.ImportAsync(importContext, exportPackage);
        }

        // Assert
        using var assertContext = factory.Create();
        var accounts = await assertContext.Accounts.OrderBy(x => x.Name).ToListAsync();
        Assert.AreEqual(2, accounts.Count);
        Assert.AreEqual("Checking", accounts[0].Name);
        Assert.AreEqual("Savings", accounts[1].Name);
        Assert.IsTrue(accounts.Single(x => x.Name == "Savings").IsDefault);
        Assert.IsNull(await assertContext.Accounts.SingleOrDefaultAsync(x => x.Name == "Old Account"));

        var categories = await assertContext.ExpenseCategories.OrderBy(x => x.Name).ToListAsync();
        Assert.AreEqual(2, categories.Count);
        var groceries = categories.Single(x => x.Name == "Groceries");
        var emergency = categories.Single(x => x.Name == "Emergency Fund");

        Assert.AreEqual(200_00, groceries.CurrentBalance);
        Assert.AreEqual(300_00, emergency.CurrentBalance);

        var rules = await assertContext.ExpenseCategoryRules
            .Include(x => x.ExpenseCategory)
            .ToListAsync();
        Assert.AreEqual(1, rules.Count);
        Assert.AreEqual("Groceries", rules[0].ExpenseCategory?.Name);

        Assert.AreEqual(1, await assertContext.Metadatas.CountAsync());
        Assert.AreEqual(2, await assertContext.ExpenseCategoryItems.CountAsync());
        Assert.AreEqual(3, await assertContext.ExpenseCategoryItemDetails.CountAsync());
        Assert.AreEqual("March salary", (await assertContext.ExpenseCategoryItems.SingleAsync(x => x.Description == "Paycheck")).Notes);
    }

    [TestMethod]
    public async Task ImportAsync_WithUnsupportedFormatVersion_Throws()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();
        using var context = factory.Create();

        var exportPackage = new BudgetDataExportPackage
        {
            FormatVersion = BudgetDataExportPackage.CurrentFormatVersion + 1
        };

        // Act / Assert
        try
        {
            await BudgetDataPortabilityService.ImportAsync(context, exportPackage);
            Assert.Fail("Expected import to throw for an unsupported format version.");
        }
        catch (InvalidOperationException)
        {
            // expected
        }
    }

    [TestMethod]
    public async Task ImportAsync_WithNullCollections_TreatsThemAsEmpty()
    {
        // Arrange
        AutoMocker mocker = new();
        using var factory = mocker.WithDbScope();

        using (var setupContext = factory.Create())
        {
            setupContext.Accounts.Add(new Account { Name = "Old Data", ValidatedDate = DateTime.Today });
            await setupContext.SaveChangesAsync();
        }

        var exportPackage = new BudgetDataExportPackage
        {
            Source = "desktop",
            Accounts = null!,
            Categories = null!,
            Items = null!,
            ItemDetails = null!,
            Rules = null!,
            Metadata = null!
        };

        // Act
        using (var importContext = factory.Create())
        {
            await BudgetDataPortabilityService.ImportAsync(importContext, exportPackage);
        }

        // Assert
        using var assertContext = factory.Create();
        Assert.AreEqual(0, await assertContext.Accounts.CountAsync());
        Assert.AreEqual(0, await assertContext.ExpenseCategories.CountAsync());
        Assert.AreEqual(0, await assertContext.ExpenseCategoryItems.CountAsync());
        Assert.AreEqual(0, await assertContext.ExpenseCategoryItemDetails.CountAsync());
        Assert.AreEqual(0, await assertContext.ExpenseCategoryRules.CountAsync());
        Assert.AreEqual(0, await assertContext.Metadatas.CountAsync());
    }
}

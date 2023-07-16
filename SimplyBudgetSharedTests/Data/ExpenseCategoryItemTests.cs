using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;
using System;
using System.Linq;

namespace SimplyBudgetSharedTests.Data;

[TestClass]
public class ExpenseCategoryItemTests
{
    [TestMethod]
    public void IgnoreBudget_WithAllDetailsTrue_ReturnsTrue()
    {
        var item = new ExpenseCategoryItem
        {
            Details = new()
            {
                new ExpenseCategoryItemDetail() { IgnoreBudget = true },
                new ExpenseCategoryItemDetail() { IgnoreBudget = true }
            }
        };

        Assert.IsTrue(item.IgnoreBudget);
    }

    [TestMethod]
    public void IgnoreBudget_WithAllDetailsFalse_ReturnsFalse()
    {
        var item = new ExpenseCategoryItem
        {
            Details = new()
            {
                new ExpenseCategoryItemDetail() { IgnoreBudget = false },
                new ExpenseCategoryItemDetail() { IgnoreBudget = false }
            }
        };

        Assert.IsFalse(item.IgnoreBudget);
    }

    [TestMethod]
    public void IgnoreBudget_WithMixedDetails_ReturnsNull()
    {
        var item = new ExpenseCategoryItem
        {
            Details = new()
            {
                new ExpenseCategoryItemDetail() { IgnoreBudget = true },
                new ExpenseCategoryItemDetail() { IgnoreBudget = false }
            }
        };

        Assert.IsNull(item.IgnoreBudget);
    }

    [TestMethod]
    public void IgnoreBudget_WithNoDetails_ReturnsNull()
    {
        var item = new ExpenseCategoryItem();

        Assert.IsNull(item.IgnoreBudget);
    }

    [TestMethod]
    public void SetIgnoreBudget_WithNull_Throws()
    {
        var item = new ExpenseCategoryItem();

        var ex = Assert.ThrowsException<ArgumentNullException>(() => item.IgnoreBudget = null);
        Assert.AreEqual("value", ex.ParamName);
    }

    [TestMethod]
    public void SetIgnoreBudget_True_Throws()
    {
        var item = new ExpenseCategoryItem();

        var ex = Assert.ThrowsException<ArgumentNullException>(() => item.IgnoreBudget = null);
        Assert.AreEqual("value", ex.ParamName);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SetIgnoreBudget_SetValue_AppliesToAllDetails(bool value)
    {
        var item = new ExpenseCategoryItem
        {
            Details = new()
            {
                new ExpenseCategoryItemDetail() { IgnoreBudget = true },
                new ExpenseCategoryItemDetail() { IgnoreBudget = false }
            }
        };

        item.IgnoreBudget = value;

        Assert.AreEqual(value, item.IgnoreBudget);
        Assert.IsTrue(item.Details.All(x => x.IgnoreBudget == value));
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimplyBudgetShared.Data;

namespace SimplyBudgetSharedTests.Data;

[TestClass]
public class BaseItemTests
{
    [TestMethod]
    public void EqualsComparesIDs()
    {
        var item1 = new TestableBaseItem { ID = 1 };
        var item2 = new TestableBaseItem { ID = 2 };
        var item3 = new TestableBaseItem { ID = 1 };

        Assert.IsFalse(item1.Equals(item2));
        Assert.IsFalse(item2.Equals(item1));
        Assert.IsTrue(item1.Equals(item3));
        Assert.IsTrue(item3.Equals(item1));
    }

    [TestMethod]
    public void EqualsHandlesOtherTypes()
    {
        Assert.IsFalse(new TestableBaseItem().Equals(new object()));
    }

    [TestMethod]
    public void HashCodeIsID()
    {
        var item1 = new TestableBaseItem { ID = 1 };
        var item2 = new TestableBaseItem { ID = 2 };

        Assert.AreEqual(1, item1.GetHashCode());
        Assert.AreEqual(2, item2.GetHashCode());
    }

    private class TestableBaseItem : BaseItem
    {

    }
}

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
    public void GetHashCode_ReturnsSameValue_ForEqualObjects()
    {
        TestableBaseItem item1 = new() { ID = 1 };
        TestableBaseItem item2 = new() { ID = 1 };

        Assert.AreEqual(item1.GetHashCode(), item2.GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_ReturnsDifferentValues_ForEqualObjectsOfDifferentTypes()
    {
        TestableBaseItem item1 = new() { ID = 1 };
        TestableBaseItem2 item2 = new() { ID = 1 };

        Assert.AreNotEqual(item1.GetHashCode(), item2.GetHashCode());
    }

    private class TestableBaseItem : BaseItem
    {

    }

    private class TestableBaseItem2 : BaseItem
    {

    }
}

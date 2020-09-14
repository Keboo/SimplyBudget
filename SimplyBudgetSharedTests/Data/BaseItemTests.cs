using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SQLite;
using SimplyBudgetShared.Data;
using Telerik.JustMock;

namespace SimplyBudgetSharedTests.Data
{
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

        [TestMethod]
        public void GetConnectionUsesDatabaseManager()
        {
            //Arrange
            var manager = Mock.Create<DatabaseManager>(Behavior.Strict);
            Mock.SetupStatic(typeof(DatabaseManager), StaticConstructor.Mocked);
            Mock.Arrange(() => DatabaseManager.Instance).Returns(manager);
            var connection = Mock.Create<SQLiteAsyncConnection>(Behavior.Strict);
            Mock.Arrange(() => manager.Connection).Returns(connection);

            var item = new TestableBaseItem();

            //Act
            var foundConnection = item.TestGetConnection();

            //Assert
            Assert.IsTrue(ReferenceEquals(connection, foundConnection));
        }

        [TestMethod]
        public async Task TestDelete()
        {
            //Arrange
            var item = new TestableBaseItem();
            var connection = CommonActions.MockConnection();
            Mock.Arrange(() => connection.DeleteAsync(item)).Returns(Task.FromResult(0));

            //Act
            await item.Delete();

            //Assert
            Mock.Assert(() => connection.DeleteAsync(item), Occurs.Once());
        }

        [TestMethod]
        public async Task SaveInsertsTransientItem()
        {
            //Arrange
            var item = new TestableBaseItem();
            var connection = CommonActions.MockConnection();
            Mock.Arrange(() => connection.InsertAsync(item)).Returns(Task.FromResult(0));

            //Act
            await item.Save();

            //Assert
            Mock.Assert(() => connection.InsertAsync(item), Occurs.Once());
        }

        [TestMethod]
        public async Task SaveUpdatesExistingItem()
        {
            //Arrange
            var item = new TestableBaseItem { ID = 1 };
            var connection = CommonActions.MockConnection();
            Mock.Arrange(() => connection.UpdateAsync(item)).Returns(Task.FromResult(0));

            //Act
            await item.Save();

            //Assert
            Mock.Assert(() => connection.UpdateAsync(item), Occurs.Once());
        }

        private class TestableBaseItem : BaseItem
        {
            public SQLiteAsyncConnection TestGetConnection()
            {
                return GetConnection();
            }
        }
    }
}

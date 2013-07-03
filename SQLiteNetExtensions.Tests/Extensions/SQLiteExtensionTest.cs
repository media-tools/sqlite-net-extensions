using Cirrious.MvvmCross.Plugins.Sqlite;
using NUnit.Framework;
using SQLiteNetExtensions.Attributes;

namespace SQLiteNetExtensions.Tests.Extensions
{
    public class ClassA
    {
        [ForeignKey(typeof(ClassB))]
        public int OneClassBKey { get; set; }

        [OneToOne]
        public ClassB OneClassB { get; set; }
    }

    public class ClassB
    {
    }

    [TestFixture]
    public class SQLiteExtensionTest
    {
        private ISQLiteConnection _connection;

        [SetUp]
        public void PrepareDatabase()
        {
        }


        [Test]
        public void TestGetOneToOne()
        {

        }
    }
}

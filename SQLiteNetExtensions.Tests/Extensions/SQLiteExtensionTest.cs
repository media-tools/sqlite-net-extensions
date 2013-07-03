using NUnit.Framework;
using SQLiteNetExtensions.Attributes;

#if USING_MVVMCROSS
using Cirrious.MvvmCross.Plugins.Sqlite;
#else
using SQLite;
#endif

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
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
    }

    [TestFixture]
    public class SQLiteExtensionTest
    {

        [Test]
        public void TestGetOneToOne()
        {

            
        }
    }
}

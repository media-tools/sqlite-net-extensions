using System;
using Cirrious.MvvmCross.Plugins.Sqlite;
using NUnit.Framework;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;

#if USING_MVVMCROSS
using Cirrious.MvvmCross.Plugins.Sqlite;
#endif

namespace SQLiteNetExtensions.IntegrationTests
{
    public class M2OClassA
    {
        [ForeignKey(typeof(M2OClassB))]
        public int OneClassBKey { get; set; }

        [ManyToOne]
        public M2OClassB OneClassB { get; set; }
    }

    public class M2OClassB
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Foo { get; set; }
    }

    [TestFixture]
    public class ManyToOneTests
    {

        [Test]
        public void TestGetManyToOne()
        {
            var conn = new SQLiteConnection("database");
            conn.DropTable<M2OClassA>();
            conn.DropTable<M2OClassB>();
            conn.CreateTable<M2OClassA>();
            conn.CreateTable<M2OClassB>();

            // Use standard SQLite-Net API to create a new relationship
            var objectB = new M2OClassB
                {
                    Foo = string.Format("Foo String {0}", new Random().Next(100))
                };
            conn.Insert(objectB);

            var objectA = new M2OClassA();
            conn.Insert(objectA);

            Assert.Null(objectA.OneClassB);

            // Fetch (yet empty) the relationship
            conn.GetChildren(ref objectA);
            Assert.Null(objectA.OneClassB);

            // Set the relationship using IDs
            objectA.OneClassBKey = objectB.Id;

            Assert.Null(objectA.OneClassB);

            // Fetch the relationship
            conn.GetChildren(ref objectA);

            Assert.NotNull(objectA.OneClassB);
            Assert.AreEqual(objectB.Id, objectA.OneClassB.Id);
            Assert.AreEqual(objectB.Foo, objectA.OneClassB.Foo);
        }

    }
}

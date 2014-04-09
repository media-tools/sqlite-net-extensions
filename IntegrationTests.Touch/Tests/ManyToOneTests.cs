using System;
using NUnit.Framework;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;

#if PCL
using SQLite.Net;
using SQLite.Net.Attributes;
#else
using Cirrious.MvvmCross.Community.Plugins.Sqlite;
using Community.SQLite;
#endif

namespace SQLiteNetExtensions.IntegrationTests
{
    
    [TestFixture]
    public class ManyToOneTests
    {
        public class M2OClassA
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [ForeignKey(typeof(M2OClassB))]
            public int OneClassBKey { get; set; }

            [ManyToOne]
            public M2OClassB OneClassB { get; set; }
        }

        [Table("m2o_class_b")]
        public class M2OClassB
        {
            [PrimaryKey, AutoIncrement, Column("_id_")]
            public int Id { get; set; }

            public string Foo { get; set; }
        }

        [Test]
        public void TestGetManyToOne()
        {
            var conn = Utils.CreateConnection();
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
            conn.GetChildren(objectA);
            Assert.Null(objectA.OneClassB);

            // Set the relationship using IDs
            objectA.OneClassBKey = objectB.Id;

            Assert.Null(objectA.OneClassB);

            // Fetch the relationship
            conn.GetChildren(objectA);

            Assert.NotNull(objectA.OneClassB);
            Assert.AreEqual(objectB.Id, objectA.OneClassB.Id);
            Assert.AreEqual(objectB.Foo, objectA.OneClassB.Foo);
        }

        [Test]
        public void TestUpdateSetManyToOne()
        {
            var conn = Utils.CreateConnection();
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
            Assert.AreEqual(0, objectA.OneClassBKey);

            objectA.OneClassB = objectB;
            Assert.AreEqual(0, objectA.OneClassBKey);

            conn.UpdateWithChildren(objectA);

            Assert.AreEqual(objectB.Id, objectA.OneClassBKey);

            var newObjectA = conn.Get<M2OClassA>(objectA.Id);
            Assert.AreEqual(objectB.Id, newObjectA.OneClassBKey);
        }

        [Test]
        public void TestUpdateUnsetManyToOne()
        {
            var conn = Utils.CreateConnection();
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
            Assert.AreEqual(0, objectA.OneClassBKey);

            objectA.OneClassB = objectB;
            Assert.AreEqual(0, objectA.OneClassBKey);

            conn.UpdateWithChildren(objectA);

            // Until here, the test is exactly the same as TestUpdateSetManyToOne
            objectA.OneClassB = null;   // Unset the relationship
            Assert.AreEqual(objectB.Id, objectA.OneClassBKey, "Foreign key shouldn't have been refreshed yet");

            conn.UpdateWithChildren(objectA);
            Assert.AreEqual(0, objectA.OneClassBKey);
            Assert.Null(objectA.OneClassB);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cirrious.MvvmCross.Plugins.Sqlite;
using NUnit.Framework;
using SQLite;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;

namespace SQLiteNetExtensions.IntegrationTests
{

    [TestFixture]
    public class OneToManyTests
    {
        public class O2MClassA
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [OneToMany]
            public List<O2MClassB> BObjects { get; set; }

            public string Bar { get; set; }
        }

        public class O2MClassB
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [ForeignKey(typeof (O2MClassA))]
            public int ClassAKey { get; set; }

            public string Foo { get; set; }
        }

        public class O2MClassC
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [OneToMany]
            public List<O2MClassD> DObjects { get; set; }

            public string Bar { get; set; }
        }

        public class O2MClassD
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [ForeignKey(typeof(O2MClassC))]
            public int ClassCKey { get; set; }

            [ManyToOne]     // OneToMany Inverse relationship
            public O2MClassC ObjectC { get; set; }

            public string Foo { get; set; }
        }

        [Test]
        public void TestGetOneToMany()
        {
            var conn = new SQLiteConnection("database");
            conn.DropTable<O2MClassA>();
            conn.DropTable<O2MClassB>();
            conn.CreateTable<O2MClassA>();
            conn.CreateTable<O2MClassB>();

            // Use standard SQLite-Net API to create the objects
            var objectsB = new List<O2MClassB>
            {
                new O2MClassB {
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new O2MClassB {
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new O2MClassB {
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new O2MClassB {
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsB);

            var objectA = new O2MClassA();
            conn.Insert(objectA);

            Assert.Null(objectA.BObjects);

            // Fetch (yet empty) the relationship
            conn.GetChildren(ref objectA);
            Assert.NotNull(objectA.BObjects);
            Assert.AreEqual(0, objectA.BObjects.Count);

            // Set the relationship using IDs
            foreach (var objectB in objectsB)
            {
                objectB.ClassAKey = objectA.Id;
                conn.Update(objectB);
            }

            Assert.NotNull(objectA.BObjects);
            Assert.AreEqual(0, objectA.BObjects.Count);

            // Fetch the relationship
            conn.GetChildren(ref objectA);

            Assert.NotNull(objectA.BObjects);
            Assert.AreEqual(objectsB.Count, objectA.BObjects.Count);
            var foos = objectsB.Select(objectB => objectB.Foo).ToList();
            foreach (var objectB in objectA.BObjects)
            {
                Assert.IsTrue(foos.Contains(objectB.Foo));
            }
        }
    }
}

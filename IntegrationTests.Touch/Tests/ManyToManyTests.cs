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
    public class ManyToManyTests
    {
        public class M2MClassA
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [ManyToMany(typeof(ClassAClassB))]
            public List<M2MClassB> BObjects { get; set; }

            public string Bar { get; set; }
        }

        public class M2MClassB
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Foo { get; set; }
        }

        public class ClassAClassB
        {
            [ForeignKey(typeof(M2MClassA))]
            public int ClassAId { get; set; }

            [ForeignKey(typeof(M2MClassB))]
            public int ClassBId { get; set; }
        }


        [Test]
        public void TestGetOneToManyList()
        {
            // In this test we will create a N:M relationship between objects of ClassA and ClassB
            //      Class A     -       Class B
            // --------------------------------------
            //          1       -       1
            //          2       -       1, 2
            //          3       -       1, 2, 3
            //          4       -       1, 2, 3, 4

            var conn = new SQLiteConnection("database");
            conn.DropTable<M2MClassA>();
            conn.DropTable<M2MClassB>();
            conn.DropTable<ClassAClassB>();
            conn.CreateTable<M2MClassA>();
            conn.CreateTable<M2MClassB>();
            conn.CreateTable<ClassAClassB>();

            // Use standard SQLite-Net API to create the objects
            var objectsB = new List<M2MClassB>
            {
                new M2MClassB {
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new M2MClassB {
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new M2MClassB {
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new M2MClassB {
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsB);

            var objectsA = new List<M2MClassA>
            {
                new M2MClassA {
                    Bar = string.Format("1- Bar String {0}", new Random().Next(100))
                },
                new M2MClassA {
                    Bar = string.Format("2- Bar String {0}", new Random().Next(100))
                },
                new M2MClassA {
                    Bar = string.Format("3- Bar String {0}", new Random().Next(100))
                },
                new M2MClassA {
                    Bar = string.Format("4- Bar String {0}", new Random().Next(100))
                }
            };

            conn.InsertAll(objectsA);

            foreach (var objectA in objectsA)
            {
                var copyA = objectA;
                Assert.Null(objectA.BObjects);

                // Fetch (yet empty) the relationship
                conn.GetChildren(ref copyA);

                Assert.NotNull(copyA.BObjects);
                Assert.AreEqual(0, copyA.BObjects.Count);
            }


            // Create the relationships in the intermediate table
            for (var aIndex = 0; aIndex < objectsA.Count; aIndex++)
            {
                for (var bIndex = 0; bIndex <= aIndex; bIndex++)
                {
                    conn.Insert(new ClassAClassB
                        {
                            ClassAId = objectsA[aIndex].Id,
                            ClassBId = objectsB[bIndex].Id
                        });
                }
            }


            for (var i = 0; i < objectsA.Count; i++)
            {
                var objectA = objectsA[i];

                // Relationship still empty because hasn't been refreshed
                Assert.NotNull(objectA.BObjects);
                Assert.AreEqual(0, objectA.BObjects.Count);

                // Fetch the relationship
                conn.GetChildren(ref objectA);

                var childrenCount = i + 1;

                Assert.NotNull(objectA.BObjects);
                Assert.AreEqual(childrenCount, objectA.BObjects.Count);
                var foos = objectsB.GetRange(0, childrenCount).Select(objectB => objectB.Foo).ToList();
                foreach (var objectB in objectA.BObjects)
                {
                    Assert.IsTrue(foos.Contains(objectB.Foo));
                }
            }
        }

    }
}

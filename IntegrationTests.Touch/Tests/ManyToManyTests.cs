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

        public class M2MClassC
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [ManyToMany(typeof(ClassCClassD), inverseForeignKey:"ClassCId")]   // Foreign key specified in ManyToMany attribute
            public M2MClassD[] DObjects { get; set; } // Array instead of List

            public string Bar { get; set; }
        }

        public class M2MClassD
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Foo { get; set; }
        }

        public class ClassCClassD
        {
            public int ClassCId { get; set; }   // ForeignKey attribute not needed, already specified in the ManyToMany relationship
            [ForeignKey(typeof(M2MClassD))]
            public int ClassDId { get; set; }
        }


        [Test]
        public void TestGetManyToManyList()
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

        [Test]
        public void TestGetManyToManyArray()
        {
            // In this test we will create a N:M relationship between objects of ClassA and ClassB
            //      Class C     -       Class D
            // --------------------------------------
            //          1       -       1
            //          2       -       1, 2
            //          3       -       1, 2, 3
            //          4       -       1, 2, 3, 4

            var conn = new SQLiteConnection("database");
            conn.DropTable<M2MClassC>();
            conn.DropTable<M2MClassD>();
            conn.DropTable<ClassCClassD>();
            conn.CreateTable<M2MClassC>();
            conn.CreateTable<M2MClassD>();
            conn.CreateTable<ClassCClassD>();

            // Use standard SQLite-Net API to create the objects
            var objectsD = new List<M2MClassD>
            {
                new M2MClassD {
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new M2MClassD {
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new M2MClassD {
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new M2MClassD {
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsD);

            var objectsC = new List<M2MClassC>
            {
                new M2MClassC {
                    Bar = string.Format("1- Bar String {0}", new Random().Next(100))
                },
                new M2MClassC {
                    Bar = string.Format("2- Bar String {0}", new Random().Next(100))
                },
                new M2MClassC {
                    Bar = string.Format("3- Bar String {0}", new Random().Next(100))
                },
                new M2MClassC {
                    Bar = string.Format("4- Bar String {0}", new Random().Next(100))
                }
            };

            conn.InsertAll(objectsC);

            foreach (var objectC in objectsC)
            {
                var copyC = objectC;
                Assert.Null(objectC.DObjects);

                // Fetch (yet empty) the relationship
                conn.GetChildren(ref copyC);

                Assert.NotNull(copyC.DObjects);
                Assert.AreEqual(0, copyC.DObjects.Length);
            }


            // Create the relationships in the intermediate table
            for (var cIndex = 0; cIndex < objectsC.Count; cIndex++)
            {
                for (var dIndex = 0; dIndex <= cIndex; dIndex++)
                {
                    conn.Insert(new ClassCClassD
                    {
                        ClassCId = objectsC[cIndex].Id,
                        ClassDId = objectsD[dIndex].Id
                    });
                }
            }


            for (var i = 0; i < objectsC.Count; i++)
            {
                var objectC = objectsC[i];

                // Relationship still empty because hasn't been refreshed
                Assert.NotNull(objectC.DObjects);
                Assert.AreEqual(0, objectC.DObjects.Length);

                // Fetch the relationship
                conn.GetChildren(ref objectC);

                var childrenCount = i + 1;

                Assert.NotNull(objectC.DObjects);
                Assert.AreEqual(childrenCount, objectC.DObjects.Length);
                var foos = objectsD.GetRange(0, childrenCount).Select(objectB => objectB.Foo).ToList();
                foreach (var objectD in objectC.DObjects)
                {
                    Assert.IsTrue(foos.Contains(objectD.Foo));
                }
            }
        }

        [Test]
        public void TestUpdateSetManyToManyList()
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
                    Bar = string.Format("1- Bar String {0}", new Random().Next(100)),
                    BObjects = new List<M2MClassB>()
                },
                new M2MClassA {
                    Bar = string.Format("2- Bar String {0}", new Random().Next(100)),
                    BObjects = new List<M2MClassB>()
                },
                new M2MClassA {
                    Bar = string.Format("3- Bar String {0}", new Random().Next(100)),
                    BObjects = new List<M2MClassB>()
                },
                new M2MClassA {
                    Bar = string.Format("4- Bar String {0}", new Random().Next(100)),
                    BObjects = new List<M2MClassB>()
                }
            };

            conn.InsertAll(objectsA);

            // Create the relationships
            for (var aIndex = 0; aIndex < objectsA.Count; aIndex++)
            {
                var objectA = objectsA[aIndex];

                for (var bIndex = 0; bIndex <= aIndex; bIndex++)
                {
                    var objectB = objectsB[bIndex];
                    objectA.BObjects.Add(objectB);
                }
            
                conn.UpdateWithChildren(objectA);
            }


            for (var i = 0; i < objectsA.Count; i++)
            {
                var objectA = objectsA[i];
                var childrenCount = i + 1;
                var storedChildKeyList = (from ClassAClassB ab in conn.Table<ClassAClassB>()
                                          where ab.ClassAId == objectA.Id
                                          select ab.ClassBId).ToList();
                                         

                Assert.AreEqual(childrenCount, storedChildKeyList.Count, "Relationship count is not correct");
                var expectedChildIds = objectsB.GetRange(0, childrenCount).Select(objectB => objectB.Id).ToList();
                foreach (var objectBKey in storedChildKeyList)
                {
                    Assert.IsTrue(expectedChildIds.Contains(objectBKey), "Relationship ID is not correct");
                }
            }
        }

        [Test]
        public void TestUpdateUnsetManyToManyList()
        {
            // In this test we will create a N:M relationship between objects of ClassA and ClassB
            //      Class A     -       Class B
            // --------------------------------------
            //          1       -       1
            //          2       -       1, 2
            //          3       -       1, 2, 3
            //          4       -       1, 2, 3, 4

            // After that, we will remove objects 1 and 2 from relationships
            //      Class A     -       Class B
            // --------------------------------------
            //          1       -       <empty>
            //          2       -       <empty>
            //          3       -       3
            //          4       -       3, 4


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
                    Bar = string.Format("1- Bar String {0}", new Random().Next(100)),
                    BObjects = new List<M2MClassB>()
                },
                new M2MClassA {
                    Bar = string.Format("2- Bar String {0}", new Random().Next(100)),
                    BObjects = new List<M2MClassB>()
                },
                new M2MClassA {
                    Bar = string.Format("3- Bar String {0}", new Random().Next(100)),
                    BObjects = new List<M2MClassB>()
                },
                new M2MClassA {
                    Bar = string.Format("4- Bar String {0}", new Random().Next(100)),
                    BObjects = new List<M2MClassB>()
                }
            };

            conn.InsertAll(objectsA);

            // Create the relationships
            for (var aIndex = 0; aIndex < objectsA.Count; aIndex++)
            {
                var objectA = objectsA[aIndex];

                for (var bIndex = 0; bIndex <= aIndex; bIndex++)
                {
                    var objectB = objectsB[bIndex];
                    objectA.BObjects.Add(objectB);
                }

                conn.UpdateWithChildren(objectA);
            }

            // At these points all the relationships are set
            //      Class A     -       Class B
            // --------------------------------------
            //          1       -       1
            //          2       -       1, 2
            //          3       -       1, 2, 3
            //          4       -       1, 2, 3, 4

            // Now we will remove ClassB objects 1 and 2 from the relationships
            var objectsBToRemove = objectsB.GetRange(0, 2);

            foreach (var objectA in objectsA)
            {
                objectA.BObjects.RemoveAll(objectsBToRemove.Contains);
                conn.UpdateWithChildren(objectA);
            }

            // This should now be the current status of all relationships

            //      Class A     -       Class B
            // --------------------------------------
            //          1       -       <empty>
            //          2       -       <empty>
            //          3       -       3
            //          4       -       3, 4

            for (var i = 0; i < objectsA.Count; i++)
            {
                var objectA = objectsA[i];

                var storedChildKeyList = (from ClassAClassB ab in conn.Table<ClassAClassB>()
                                          where ab.ClassAId == objectA.Id
                                          select ab.ClassBId).ToList();


                var expectedChildIds = objectsB.GetRange(0, i + 1).Where(b => !objectsBToRemove.Contains(b)).Select(objectB => objectB.Id).ToList();
                Assert.AreEqual(expectedChildIds.Count, storedChildKeyList.Count, string.Format("Relationship count is not correct for Object with Id {0}", objectA.Id));
                foreach (var objectBKey in storedChildKeyList)
                {
                    Assert.IsTrue(expectedChildIds.Contains(objectBKey), "Relationship ID is not correct");
                }
            }
        }
    }
}

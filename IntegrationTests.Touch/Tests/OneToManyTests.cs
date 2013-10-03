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

        public class O2MClassE
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [OneToMany("ClassEKey")]   // Explicit foreign key declaration
            public O2MClassF[] FObjects { get; set; } // Array of objects instead of List

            public string Bar { get; set; }
        }

        public class O2MClassF
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int ClassEKey { get; set; }  // Foreign key declared in relationship

            public string Foo { get; set; }
        }

        [Test]
        public void TestGetOneToManyList()
        {
            var conn = new SQLiteConnection(Utils.DatabaseFilePath);
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

        [Test]
        public void TestGetOneToManyListWithInverse()
        {
            var conn = new SQLiteConnection(Utils.DatabaseFilePath);
            conn.DropTable<O2MClassC>();
            conn.DropTable<O2MClassD>();
            conn.CreateTable<O2MClassC>();
            conn.CreateTable<O2MClassD>();

            // Use standard SQLite-Net API to create the objects
            var objectsD = new List<O2MClassD>
            {
                new O2MClassD {
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new O2MClassD {
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new O2MClassD {
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new O2MClassD {
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsD);

            var objectC = new O2MClassC();
            conn.Insert(objectC);

            Assert.Null(objectC.DObjects);

            // Fetch (yet empty) the relationship
            conn.GetChildren(ref objectC);
            Assert.NotNull(objectC.DObjects);
            Assert.AreEqual(0, objectC.DObjects.Count);

            // Set the relationship using IDs
            foreach (var objectD in objectsD)
            {
                objectD.ClassCKey = objectC.Id;
                conn.Update(objectD);
            }

            Assert.NotNull(objectC.DObjects);
            Assert.AreEqual(0, objectC.DObjects.Count);

            // Fetch the relationship
            conn.GetChildren(ref objectC);

            Assert.NotNull(objectC.DObjects);
            Assert.AreEqual(objectsD.Count, objectC.DObjects.Count);
            var foos = objectsD.Select(objectB => objectB.Foo).ToList();
            foreach (var objectD in objectC.DObjects)
            {
                Assert.IsTrue(foos.Contains(objectD.Foo));
                Assert.AreEqual(objectC.Id, objectD.ObjectC.Id);
                Assert.AreEqual(objectC.Bar, objectD.ObjectC.Bar);
                Assert.AreSame(objectC, objectD.ObjectC); // Not only equal, they are the same!
            }
        }

        [Test]
        public void TestGetOneToManyArray()
        {
            var conn = new SQLiteConnection(Utils.DatabaseFilePath);
            conn.DropTable<O2MClassE>();
            conn.DropTable<O2MClassF>();
            conn.CreateTable<O2MClassE>();
            conn.CreateTable<O2MClassF>();

            // Use standard SQLite-Net API to create the objects
            var objectsF = new[]
            {
                new O2MClassF {
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new O2MClassF {
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new O2MClassF {
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new O2MClassF {
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsF);

            var objectE = new O2MClassE();
            conn.Insert(objectE);

            Assert.Null(objectE.FObjects);

            // Fetch (yet empty) the relationship
            conn.GetChildren(ref objectE);
            Assert.NotNull(objectE.FObjects);
            Assert.AreEqual(0, objectE.FObjects.Length);

            // Set the relationship using IDs
            foreach (var objectB in objectsF)
            {
                objectB.ClassEKey = objectE.Id;
                conn.Update(objectB);
            }

            Assert.NotNull(objectE.FObjects);
            Assert.AreEqual(0, objectE.FObjects.Length);

            // Fetch the relationship
            conn.GetChildren(ref objectE);

            Assert.NotNull(objectE.FObjects);
            Assert.AreEqual(objectsF.Length, objectE.FObjects.Length);
            var foos = objectsF.Select(objectF => objectF.Foo).ToList();
            foreach (var objectF in objectE.FObjects)
            {
                Assert.IsTrue(foos.Contains(objectF.Foo));
            }
        }

        [Test]
        public void TestUpdateSetOneToManyList()
        {
            var conn = new SQLiteConnection(Utils.DatabaseFilePath);
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

            objectA.BObjects = objectsB;

            foreach (var objectB in objectsB)
            {
                Assert.AreEqual(0, objectB.ClassAKey, "Foreign keys shouldn't have been updated yet");
            }


            conn.UpdateWithChildren(objectA);

            foreach (var objectB in objectA.BObjects)
            {
                Assert.AreEqual(objectA.Id, objectB.ClassAKey, "Foreign keys haven't been updated yet");

                // Check database values
                var newObjectB = conn.Get<O2MClassB>(objectB.Id);
                Assert.AreEqual(objectA.Id, newObjectB.ClassAKey, "Database stored value is not correct");
            }

        }

        [Test]
        public void TestUpdateUnsetOneToManyEmptyList()
        {
            var conn = new SQLiteConnection(Utils.DatabaseFilePath);
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

            objectA.BObjects = objectsB;

            foreach (var objectB in objectsB)
            {
                Assert.AreEqual(0, objectB.ClassAKey, "Foreign keys shouldn't have been updated yet");
            }

            conn.UpdateWithChildren(objectA);

            foreach (var objectB in objectA.BObjects)
            {
                Assert.AreEqual(objectA.Id, objectB.ClassAKey, "Foreign keys haven't been updated yet");

                // Check database values
                var newObjectB = conn.Get<O2MClassB>(objectB.Id);
                Assert.AreEqual(objectA.Id, newObjectB.ClassAKey, "Database stored value is not correct");
            }

            // At this point the test is exactly the same as TestUpdateSetOneToManyList
            objectA.BObjects = new List<O2MClassB>(); // Reset the relationship

            conn.UpdateWithChildren(objectA);

            foreach (var objectB in objectsB)
            {
                // Check database values
                var newObjectB = conn.Get<O2MClassB>(objectB.Id);
                Assert.AreEqual(0, newObjectB.ClassAKey, "Database stored value is not correct");
            }

        }

        [Test]
        public void TestUpdateUnsetOneToManyNullList()
        {
            var conn = new SQLiteConnection(Utils.DatabaseFilePath);
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

            objectA.BObjects = objectsB;

            foreach (var objectB in objectsB)
            {
                Assert.AreEqual(0, objectB.ClassAKey, "Foreign keys shouldn't have been updated yet");
            }

            conn.UpdateWithChildren(objectA);

            foreach (var objectB in objectA.BObjects)
            {
                Assert.AreEqual(objectA.Id, objectB.ClassAKey, "Foreign keys haven't been updated yet");

                // Check database values
                var newObjectB = conn.Get<O2MClassB>(objectB.Id);
                Assert.AreEqual(objectA.Id, newObjectB.ClassAKey, "Database stored value is not correct");
            }

            // At this point the test is exactly the same as TestUpdateSetOneToManyList
            objectA.BObjects = null; // Reset the relationship

            conn.UpdateWithChildren(objectA);

            foreach (var objectB in objectsB)
            {
                // Check database values
                var newObjectB = conn.Get<O2MClassB>(objectB.Id);
                Assert.AreEqual(0, newObjectB.ClassAKey, "Database stored value is not correct");
            }

        }

        [Test]
        public void TestUpdateSetOneToManyArray()
        {
            var conn = new SQLiteConnection(Utils.DatabaseFilePath);
            conn.DropTable<O2MClassE>();
            conn.DropTable<O2MClassF>();
            conn.CreateTable<O2MClassE>();
            conn.CreateTable<O2MClassF>();

            // Use standard SQLite-Net API to create the objects
            var objectsF = new[]
            {
                new O2MClassF {
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new O2MClassF {
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new O2MClassF {
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new O2MClassF {
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsF);

            var objectE = new O2MClassE();
            conn.Insert(objectE);

            Assert.Null(objectE.FObjects);

            objectE.FObjects = objectsF;

            foreach (var objectF in objectsF)
            {
                Assert.AreEqual(0, objectF.ClassEKey, "Foreign keys shouldn't have been updated yet");
            }


            conn.UpdateWithChildren(objectE);

            foreach (var objectF in objectE.FObjects)
            {
                Assert.AreEqual(objectE.Id, objectF.ClassEKey, "Foreign keys haven't been updated yet");

                // Check database values
                var newObjectF = conn.Get<O2MClassB>(objectF.Id);
                Assert.AreEqual(objectE.Id, newObjectF.ClassAKey, "Database stored value is not correct");
            }

        }


        [Test]
        public void TestUpdateSetOneToManyListWithInverse()
        {
            var conn = new SQLiteConnection(Utils.DatabaseFilePath);
            conn.DropTable<O2MClassC>();
            conn.DropTable<O2MClassD>();
            conn.CreateTable<O2MClassC>();
            conn.CreateTable<O2MClassD>();

            // Use standard SQLite-Net API to create the objects
            var objectsD = new List<O2MClassD>
            {
                new O2MClassD {
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new O2MClassD {
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new O2MClassD {
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new O2MClassD {
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsD);

            var objectC = new O2MClassC();
            conn.Insert(objectC);

            Assert.Null(objectC.DObjects);

            objectC.DObjects = objectsD;

            foreach (var objectD in objectsD)
            {
                Assert.AreEqual(0, objectD.ClassCKey, "Foreign keys shouldn't have been updated yet");
            }


            conn.UpdateWithChildren(objectC);

            foreach (var objectD in objectC.DObjects)
            {
                Assert.AreEqual(objectC.Id, objectD.ClassCKey, "Foreign keys haven't been updated yet");
                Assert.AreSame(objectC, objectD.ObjectC, "Inverse relationship hasn't been set");

                // Check database values
                var newObjectB = conn.Get<O2MClassB>(objectD.Id);
                Assert.AreEqual(objectC.Id, newObjectB.ClassAKey, "Database stored value is not correct");
            }

        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class OneToManyTests
    {
        [Table("ClassA")]
        public class O2MClassA
        {
            [PrimaryKey, AutoIncrement, Column("PrimaryKey")]
            public int Id { get; set; }

            [OneToMany]
            public List<O2MClassB> BObjects { get; set; }

            public string Bar { get; set; }
        }

        [Table("ClassB")]
        public class O2MClassB
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [ForeignKey(typeof (O2MClassA)), Column("class_a_id")]
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

        public class O2MClassG
        {
            [PrimaryKey]
            public Guid Guid { get; set; }

            [OneToMany]
            public List<O2MClassH> HObjects { get; set; }

            public string Bar { get; set; }
        }

        public class O2MClassH
        {
            [PrimaryKey]
            public Guid Guid { get; set; }

            [ForeignKey(typeof(O2MClassG))]
            public Guid ClassGKey { get; set; }

            [ManyToOne]     // OneToMany Inverse relationship
            public O2MClassG ObjectG { get; set; }

            public string Foo { get; set; }
        }

        [Test]
        public void TestGetOneToManyList()
        {
            var conn = Utils.CreateConnection();
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
            conn.GetChildren(objectA);
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
            conn.GetChildren(objectA);

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
            var conn = Utils.CreateConnection();
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
            conn.GetChildren(objectC);
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
            conn.GetChildren(objectC);

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
            var conn = Utils.CreateConnection();
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
            conn.GetChildren(objectE);
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
            conn.GetChildren(objectE);

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
            var conn = Utils.CreateConnection();
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
            var conn = Utils.CreateConnection();
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
            var conn = Utils.CreateConnection();
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
            var conn = Utils.CreateConnection();
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
            var conn = Utils.CreateConnection();
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
                var newObjectD = conn.Get<O2MClassD>(objectD.Id);
                Assert.AreEqual(objectC.Id, newObjectD.ClassCKey, "Database stored value is not correct");
            }

        }

        [Test]
        public void TestGetOneToManyListWithInverseGuidId()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2MClassG>();
            conn.DropTable<O2MClassH>();
            conn.CreateTable<O2MClassG>();
            conn.CreateTable<O2MClassH>();

            // Use standard SQLite-Net API to create the objects
            var objectsD = new List<O2MClassH>
            {
                new O2MClassH {
                    Guid = Guid.NewGuid(),
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new O2MClassH {
                    Guid = Guid.NewGuid(),
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new O2MClassH {
                    Guid = Guid.NewGuid(),
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new O2MClassH {
                    Guid = Guid.NewGuid(),
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsD);

            var objectC = new O2MClassG { Guid = Guid.NewGuid() };
            conn.Insert(objectC);

            Assert.Null(objectC.HObjects);

            // Fetch (yet empty) the relationship
            conn.GetChildren(objectC);
            Assert.NotNull(objectC.HObjects);
            Assert.AreEqual(0, objectC.HObjects.Count);

            // Set the relationship using IDs
            foreach (var objectD in objectsD)
            {
                objectD.ClassGKey = objectC.Guid;
                conn.Update(objectD);
            }

            Assert.NotNull(objectC.HObjects);
            Assert.AreEqual(0, objectC.HObjects.Count);

            // Fetch the relationship
            conn.GetChildren(objectC);

            Assert.NotNull(objectC.HObjects);
            Assert.AreEqual(objectsD.Count, objectC.HObjects.Count);
            var foos = objectsD.Select(objectB => objectB.Foo).ToList();
            foreach (var objectD in objectC.HObjects)
            {
                Assert.IsTrue(foos.Contains(objectD.Foo));
                Assert.AreEqual(objectC.Guid, objectD.ObjectG.Guid);
                Assert.AreEqual(objectC.Bar, objectD.ObjectG.Bar);
                Assert.AreSame(objectC, objectD.ObjectG); // Not only equal, they are the same!
            }
        }

        [Test]
        public void TestUpdateSetOneToManyListWithInverseGuidId()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2MClassG>();
            conn.DropTable<O2MClassH>();
            conn.CreateTable<O2MClassG>();
            conn.CreateTable<O2MClassH>();

            // Use standard SQLite-Net API to create the objects
            var objectsH = new List<O2MClassH>
            {
                new O2MClassH {
                    Guid = Guid.NewGuid(),
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new O2MClassH {
                    Guid = Guid.NewGuid(),
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new O2MClassH {
                    Guid = Guid.NewGuid(),
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new O2MClassH {
                    Guid = Guid.NewGuid(),
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsH);

            var objectG = new O2MClassG { Guid = Guid.NewGuid() };
            conn.Insert(objectG);

            Assert.Null(objectG.HObjects);

            objectG.HObjects = objectsH;

            foreach (var objectD in objectsH)
            {
                Assert.AreEqual(Guid.Empty, objectD.ClassGKey, "Foreign keys shouldn't have been updated yet");
            }


            conn.UpdateWithChildren(objectG);

            foreach (var objectH in objectG.HObjects)
            {
                Assert.AreEqual(objectG.Guid, objectH.ClassGKey, "Foreign keys haven't been updated yet");
                Assert.AreSame(objectG, objectH.ObjectG, "Inverse relationship hasn't been set");

                // Check database values
                var newObjectH = conn.Get<O2MClassH>(objectH.Guid);
                Assert.AreEqual(objectG.Guid, newObjectH.ClassGKey, "Database stored value is not correct");
            }

        }

        public class Employee
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Name { get; set; }

            [OneToMany]
            public List<Employee> Subordinates { get; set; }

            [ManyToOne]
            public Employee Supervisor { get; set; }

            [ForeignKey(typeof(Employee))]
            public int SupervisorId { get; set; }
        }

        /// <summary>
        /// Tests the recursive inverse relationship automatic discovery
        /// Issue #17: https://bitbucket.org/twincoders/sqlite-net-extensions/issue/17
        /// </summary>
        [Test][NUnit.Framework.Ignore]
        public void TestRecursiveInverseRelationship() {
            var conn = Utils.CreateConnection();
            conn.DropTable<Employee>();
            conn.CreateTable<Employee>();

            var employee1 = new Employee { 
                Name = "Albert" 
            };
            conn.Insert(employee1);

            var employee2 = new Employee {
                Name = "Leonardo",
                SupervisorId = employee1.Id
            };
            conn.Insert(employee2);

            var result = conn.GetWithChildren<Employee>(employee1.Id);
            Assert.AreEqual(employee1, result);
            Assert.That(employee1.Subordinates.Select(e => e.Name), Contains.Item(employee2.Name));
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;

#if PCL
using SQLite.Net;
using SQLite.Net.Attributes;
using SQLite.Net.Platform.XamarinIOS;
#else
using Cirrious.MvvmCross.Community.Plugins.Sqlite;
using Community.SQLite;
#endif

namespace SQLiteNetExtensions.IntegrationTests
{

    [TestFixture]
    public class ManyToManyTests
    {
        public class M2MClassA
        {
            [PrimaryKey, AutoIncrement, Column("_id")]
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
            [ForeignKey(typeof(M2MClassA)), Column("class_a_id")]
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

        [Table("class_e")]
        public class M2MClassE
        {
            [PrimaryKey]
            public Guid Id { get; set; } // Guid identifier instead of int

            [ManyToMany(typeof(ClassEClassF), inverseForeignKey:"ClassEId")]   // Foreign key specified in ManyToMany attribute
            public M2MClassF[] FObjects { get; set; } // Array instead of List

            public string Bar { get; set; }
        }

        public class M2MClassF
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Foo { get; set; }
        }

        [Table("class_e_class_f")]
        public class ClassEClassF
        {
            public Guid ClassEId { get; set; }   // ForeignKey attribute not needed, already specified in the ManyToMany relationship
            [ForeignKey(typeof(M2MClassF))]
            public int ClassFId { get; set; }
        }

        public class M2MClassG
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Name { get; set; }

            [ManyToMany(typeof(ClassGClassG), "ChildId", "Children")]
            public List<M2MClassG> Parents { get; set; }

            [ManyToMany(typeof(ClassGClassG), "ParentId", "Parents")]
            public List<M2MClassG> Children { get; set; }
        }

        [Table("M2MClassG_ClassG")]
        public class ClassGClassG
        {
            [Column("Identifier")]
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [Column("parent_id")]
            public int ParentId { get; set; }
            public int ChildId { get; set; }
        }

        [Table("ClassH")]
        public class M2MClassH
        {
            [Column("_id")]
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Name { get; set; }

            [Column("parent_elements")]
            [ManyToMany(typeof(ClassHClassH), "ChildId", "Children", ReadOnly = true)] // Parents relationship is read only
            public List<M2MClassH> Parents { get; set; }

            [ManyToMany(typeof(ClassHClassH), "ParentId", "Parents")]
            public List<M2MClassH> Children { get; set; }
        }

        public class ClassHClassH
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int ParentId { get; set; }
            public int ChildId { get; set; }
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

            var conn = Utils.CreateConnection();
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
                conn.GetChildren(copyA);

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
                conn.GetChildren(objectA);

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
            // In this test we will create a N:M relationship between objects of ClassC and ClassD
            //      Class C     -       Class D
            // --------------------------------------
            //          1       -       1
            //          2       -       1, 2
            //          3       -       1, 2, 3
            //          4       -       1, 2, 3, 4

            var conn = Utils.CreateConnection();
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
                conn.GetChildren(copyC);

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
                conn.GetChildren(objectC);

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

            var conn = Utils.CreateConnection();
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


            var conn = Utils.CreateConnection();
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

        [Test]
        public void TestGetManyToManyGuidIdentifier()
        {
            // In this test we will create a N:M relationship between objects of ClassE and ClassF
            //      Class E     -       Class F
            // --------------------------------------
            //          1       -       1
            //          2       -       1, 2
            //          3       -       1, 2, 3
            //          4       -       1, 2, 3, 4

            var conn = Utils.CreateConnection();
            conn.DropTable<M2MClassE>();
            conn.DropTable<M2MClassF>();
            conn.DropTable<ClassEClassF>();
            conn.CreateTable<M2MClassE>();
            conn.CreateTable<M2MClassF>();
            conn.CreateTable<ClassEClassF>();

            // Use standard SQLite-Net API to create the objects
            var objectsF = new List<M2MClassF>
            {
                new M2MClassF {
                    Foo = string.Format("1- Foo String {0}", new Random().Next(100))
                },
                new M2MClassF {
                    Foo = string.Format("2- Foo String {0}", new Random().Next(100))
                },
                new M2MClassF {
                    Foo = string.Format("3- Foo String {0}", new Random().Next(100))
                },
                new M2MClassF {
                    Foo = string.Format("4- Foo String {0}", new Random().Next(100))
                }
            };
            conn.InsertAll(objectsF);

            var objectsE = new List<M2MClassE>
            {
                new M2MClassE {
                    Id = Guid.NewGuid(),
                    Bar = string.Format("1- Bar String {0}", new Random().Next(100))
                },
                new M2MClassE {
                    Id = Guid.NewGuid(),
                    Bar = string.Format("2- Bar String {0}", new Random().Next(100))
                },
                new M2MClassE {
                    Id = Guid.NewGuid(),
                    Bar = string.Format("3- Bar String {0}", new Random().Next(100))
                },
                new M2MClassE {
                    Id = Guid.NewGuid(),
                    Bar = string.Format("4- Bar String {0}", new Random().Next(100))
                }
            };

            conn.InsertAll(objectsE);

            foreach (var objectE in objectsE)
            {
                var copyE = objectE;
                Assert.Null(objectE.FObjects);

                // Fetch (yet empty) the relationship
                conn.GetChildren(copyE);

                Assert.NotNull(copyE.FObjects);
                Assert.AreEqual(0, copyE.FObjects.Length);
            }


            // Create the relationships in the intermediate table
            for (var eIndex = 0; eIndex < objectsE.Count; eIndex++)
            {
                for (var fIndex = 0; fIndex <= eIndex; fIndex++)
                {
                    conn.Insert(new ClassEClassF
                        {
                            ClassEId = objectsE[eIndex].Id,
                            ClassFId = objectsF[fIndex].Id
                        });
                }
            }


            for (var i = 0; i < objectsE.Count; i++)
            {
                var objectE = objectsE[i];

                // Relationship still empty because hasn't been refreshed
                Assert.NotNull(objectE.FObjects);
                Assert.AreEqual(0, objectE.FObjects.Length);

                // Fetch the relationship
                conn.GetChildren(objectE);

                var childrenCount = i + 1;

                Assert.NotNull(objectE.FObjects);
                Assert.AreEqual(childrenCount, objectE.FObjects.Length);
                var foos = objectsF.GetRange(0, childrenCount).Select(objectB => objectB.Foo).ToList();
                foreach (var objectD in objectE.FObjects)
                {
                    Assert.IsTrue(foos.Contains(objectD.Foo));
                }
            }
        }

        [Test]
        public void TestManyToManyCircular() {
            // In this test we will create a many to many relationship between instances of the same class
            // including inverse relationship

            // This is the hierarchy that we're going to implement
            //                      1
            //                     / \
            //                   [2] [3]
            //                  /  \ /  \
            //                 4    5    6
            //
            // To implement it, only relationshipd of objects [2] and [3] are going to be persisted,
            // the inverse relationships will be discovered automatically

            var conn = Utils.CreateConnection();
            conn.DropTable<M2MClassG>();
            conn.DropTable<ClassGClassG>();
            conn.CreateTable<M2MClassG>();
            conn.CreateTable<ClassGClassG>();

            var object1 = new M2MClassG { Name = "Object 1" };
            var object2 = new M2MClassG { Name = "Object 2" };
            var object3 = new M2MClassG { Name = "Object 3" };
            var object4 = new M2MClassG { Name = "Object 4" };
            var object5 = new M2MClassG { Name = "Object 5" };
            var object6 = new M2MClassG { Name = "Object 6" };

            var objects = new List<M2MClassG>{ object1, object2, object3, object4, object5, object6 };
            conn.InsertAll(objects);

            object2.Parents = new List<M2MClassG>{ object1 };
            object2.Children = new List<M2MClassG>{ object4, object5 };
            conn.UpdateWithChildren(object2);

            object3.Parents = new List<M2MClassG>{ object1 };
            object3.Children = new List<M2MClassG>{ object5, object6 };
            conn.UpdateWithChildren(object3);

            // These relationships are discovered on runtime, assign them to check for correctness below
            object1.Children = new List<M2MClassG>{ object2, object3 };
            object4.Parents = new List<M2MClassG>{ object2 };
            object5.Parents = new List<M2MClassG>{ object2, object3 };
            object6.Parents = new List<M2MClassG>{ object3 };

            foreach (var expected in objects)
            {
                var obtained = conn.GetWithChildren<M2MClassG>(expected.Id);

                Assert.AreEqual(expected.Name, obtained.Name);
                Assert.AreEqual((expected.Children ?? new List<M2MClassG>()).Count, (obtained.Children ?? new List<M2MClassG>()).Count, obtained.Name);
                Assert.AreEqual((expected.Parents ?? new List<M2MClassG>()).Count, (obtained.Parents ?? new List<M2MClassG>()).Count, obtained.Name);

                foreach (var child in expected.Children ?? Enumerable.Empty<M2MClassG>())
                    Assert.IsTrue(obtained.Children.Any(c => c.Id == child.Id && c.Name == child.Name), obtained.Name);

                foreach (var parent in expected.Parents ?? Enumerable.Empty<M2MClassG>())
                    Assert.IsTrue(obtained.Parents.Any(p => p.Id == parent.Id && p.Name == parent.Name), obtained.Name);
            }
        }

        [Test]
        public void TestManyToManyCircularReadOnly() {
            // In this test we will create a many to many relationship between instances of the same class
            // including inverse relationship

            // This is the hierarchy that we're going to implement
            //                     [1]
            //                     / \
            //                   [2] [3]
            //                  /  \ /  \
            //                 4    5    6
            //
            // To implement it, only children relationshipd of objects [1], [2] and [3] are going to be persisted,
            // the inverse relationships will be discovered automatically

            var conn = Utils.CreateConnection();
            conn.DropTable<M2MClassH>();
            conn.DropTable<ClassHClassH>();
            conn.CreateTable<M2MClassH>();
            conn.CreateTable<ClassHClassH>();

            var object1 = new M2MClassH { Name = "Object 1" };
            var object2 = new M2MClassH { Name = "Object 2" };
            var object3 = new M2MClassH { Name = "Object 3" };
            var object4 = new M2MClassH { Name = "Object 4" };
            var object5 = new M2MClassH { Name = "Object 5" };
            var object6 = new M2MClassH { Name = "Object 6" };

            var objects = new List<M2MClassH>{ object1, object2, object3, object4, object5, object6 };
            conn.InsertAll(objects);

            object1.Children = new List<M2MClassH>{ object2, object3 };
            conn.UpdateWithChildren(object1);

            object2.Children = new List<M2MClassH>{ object4, object5 };
            conn.UpdateWithChildren(object2);

            object3.Children = new List<M2MClassH>{ object5, object6 };
            conn.UpdateWithChildren(object3);

            // These relationships are discovered on runtime, assign them to check for correctness below
            object2.Parents = new List<M2MClassH>{ object1 };
            object3.Parents = new List<M2MClassH>{ object1 };
            object4.Parents = new List<M2MClassH>{ object2 };
            object5.Parents = new List<M2MClassH>{ object2, object3 };
            object6.Parents = new List<M2MClassH>{ object3 };

            foreach (var expected in objects)
            {
                var obtained = conn.GetWithChildren<M2MClassH>(expected.Id);

                Assert.AreEqual(expected.Name, obtained.Name);
                Assert.AreEqual((expected.Children ?? new List<M2MClassH>()).Count, (obtained.Children ?? new List<M2MClassH>()).Count, obtained.Name);
                Assert.AreEqual((expected.Parents ?? new List<M2MClassH>()).Count, (obtained.Parents ?? new List<M2MClassH>()).Count, obtained.Name);

                foreach (var child in expected.Children ?? Enumerable.Empty<M2MClassH>())
                    Assert.IsTrue(obtained.Children.Any(c => c.Id == child.Id && c.Name == child.Name), obtained.Name);

                foreach (var parent in expected.Parents ?? Enumerable.Empty<M2MClassH>())
                    Assert.IsTrue(obtained.Parents.Any(p => p.Id == parent.Id && p.Name == parent.Name), obtained.Name);
            }
        }
    }
}

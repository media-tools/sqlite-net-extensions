using System;
using NUnit.Framework;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;
using System.Linq;


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
    public class OneToOneTests
    {
        public class O2OClassA
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [ForeignKey(typeof(O2OClassB))]     // Explicit foreign key attribute
            public int OneClassBKey { get; set; }

            [OneToOne]
            public O2OClassB OneClassB { get; set; }
        }

        public class O2OClassB
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Foo { get; set; }
        }

        public class O2OClassC
        {
            [PrimaryKey, AutoIncrement]
            public int ClassId { get; set; }

            [OneToOne]     // OneToOne Foreign key can be declared in the referenced class
            public O2OClassD ElementD { get; set; }

            public string Bar { get; set; }
        }

        public class O2OClassD
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [ForeignKey(typeof(O2OClassC))]    // Explicit foreign key attribute for a inverse relationship
            public int ObjectCKey { get; set; }

            public string Foo { get; set; }
        }

        public class O2OClassE
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public int ObjectFKey { get; set; }

            [OneToOne("ObjectFKey")]        // Explicit foreign key declaration
            public O2OClassF ObjectF { get; set; }

            public string Foo { get; set; }
        }

        public class O2OClassF
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            [OneToOne]      // Inverse relationship, doesn't need foreign key
            public O2OClassE ObjectE { get; set; }

            public string Bar { get; set; }
        }


        [Test]
        public void TestGetOneToOneDirect()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassA>();
            conn.DropTable<O2OClassB>();
            conn.CreateTable<O2OClassA>();
            conn.CreateTable<O2OClassB>();

            // Use standard SQLite-Net API to create a new relationship
            var objectB = new O2OClassB
                {
                    Foo = string.Format("Foo String {0}", new Random().Next(100))
                };
            conn.Insert(objectB);

            var objectA = new O2OClassA();
            conn.Insert(objectA);

            Assert.Null(objectA.OneClassB);

            // Fetch (yet empty) the relationship
            conn.GetChildren(objectA);
            Assert.Null(objectA.OneClassB);

            // Set the relationship using IDs
            objectA.OneClassBKey = objectB.Id;
            conn.Update(objectA);

            Assert.Null(objectA.OneClassB);

            // Fetch the relationship
            conn.GetChildren(objectA);

            Assert.NotNull(objectA.OneClassB);
            Assert.AreEqual(objectB.Id, objectA.OneClassB.Id);
            Assert.AreEqual(objectB.Foo, objectA.OneClassB.Foo);
        }

        [Test]
        public void TestGetOneToOneInverseForeignKey()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassC>();
            conn.DropTable<O2OClassD>();
            conn.CreateTable<O2OClassC>();
            conn.CreateTable<O2OClassD>();

            // Use standard SQLite-Net API to create a new relationship
            var objectC = new O2OClassC
            {
                Bar = string.Format("Bar String {0}", new Random().Next(100))
            };
            conn.Insert(objectC);

            Assert.Null(objectC.ElementD);

            // Fetch (yet empty) the relationship
            conn.GetChildren(objectC);

            Assert.Null(objectC.ElementD);

            var objectD = new O2OClassD
            {
                ObjectCKey = objectC.ClassId,
                Foo = string.Format("Foo String {0}", new Random().Next(100))
            };
            conn.Insert(objectD);

            Assert.Null(objectC.ElementD);

            // Fetch the relationship
            conn.GetChildren(objectC);

            Assert.NotNull(objectC.ElementD);
            Assert.AreEqual(objectC.ClassId, objectC.ElementD.ObjectCKey);
            Assert.AreEqual(objectD.Foo, objectC.ElementD.Foo);
        }

        [Test]
        public void TestGetOneToOneWithInverseRelationship()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassE>();
            conn.DropTable<O2OClassF>();
            conn.CreateTable<O2OClassE>();
            conn.CreateTable<O2OClassF>();

            // Use standard SQLite-Net API to create a new relationship
            var objectF = new O2OClassF
            {
                Bar = string.Format("Bar String {0}", new Random().Next(100))
            };
            conn.Insert(objectF);

            var objectE = new O2OClassE
                {
                    Foo = string.Format("Foo String {0}", new Random().Next(100))
                };
            conn.Insert(objectE);

            Assert.Null(objectE.ObjectF);

            // Fetch (yet empty) the relationship
            conn.GetChildren(objectE);
            Assert.Null(objectE.ObjectF);

            // Set the relationship using IDs
            objectE.ObjectFKey = objectF.Id;
            conn.Update(objectE);

            Assert.Null(objectE.ObjectF);

            // Fetch the relationship
            conn.GetChildren(objectE);

            Assert.NotNull(objectE.ObjectF);
            Assert.AreEqual(objectF.Id, objectE.ObjectF.Id);
            Assert.AreEqual(objectF.Bar, objectE.ObjectF.Bar);

            // Check the inverse relationship
            Assert.NotNull(objectE.ObjectF.ObjectE);
            Assert.AreEqual(objectE.Foo, objectE.ObjectF.ObjectE.Foo);
            Assert.AreSame(objectE, objectE.ObjectF.ObjectE);
        }

        [Test]
        public void TestGetInverseOneToOneRelationshipWithExplicitKey()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassE>();
            conn.DropTable<O2OClassF>();
            conn.CreateTable<O2OClassE>();
            conn.CreateTable<O2OClassF>();

            // Use standard SQLite-Net API to create a new relationship
            var objectF = new O2OClassF
            {
                Bar = string.Format("Bar String {0}", new Random().Next(100))
            };
            conn.Insert(objectF);

            var objectE = new O2OClassE
            {
                Foo = string.Format("Foo String {0}", new Random().Next(100))
            };
            conn.Insert(objectE);

            Assert.Null(objectF.ObjectE);

            // Fetch (yet empty) the relationship
            conn.GetChildren(objectF);
            Assert.Null(objectF.ObjectE);

            // Set the relationship using IDs
            objectE.ObjectFKey = objectF.Id;
            conn.Update(objectE);

            Assert.Null(objectF.ObjectE);

            // Fetch the relationship
            conn.GetChildren(objectF);

            Assert.NotNull(objectF.ObjectE);
            Assert.AreEqual(objectE.Foo, objectF.ObjectE.Foo);

            // Check the inverse relationship
            Assert.NotNull(objectF.ObjectE.ObjectF);
            Assert.AreEqual(objectF.Id, objectF.ObjectE.ObjectF.Id);
            Assert.AreEqual(objectF.Bar, objectF.ObjectE.ObjectF.Bar);
            Assert.AreSame(objectF, objectF.ObjectE.ObjectF);
        }

        [Test]
        public void TestUpdateSetOneToOneRelationship()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassA>();
            conn.DropTable<O2OClassB>();
            conn.CreateTable<O2OClassA>();
            conn.CreateTable<O2OClassB>();

            // Use standard SQLite-Net API to create a new relationship
            var objectB = new O2OClassB
            {
                Foo = string.Format("Foo String {0}", new Random().Next(100))
            };
            conn.Insert(objectB);

            var objectA = new O2OClassA();
            conn.Insert(objectA);

            // Set the relationship using objects
            objectA.OneClassB = objectB;
            Assert.AreEqual(0, objectA.OneClassBKey);

            conn.UpdateWithChildren(objectA);

            Assert.AreEqual(objectB.Id, objectA.OneClassBKey, "Foreign key should have been refreshed");

            // Fetch the relationship
            var newObjectA = conn.Get<O2OClassA>(objectA.Id);
            Assert.AreEqual(objectB.Id, newObjectA.OneClassBKey, "Foreign key should have been refreshed in database");

        }

        [Test]
        public void TestUpdateUnsetOneToOneRelationship()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassA>();
            conn.DropTable<O2OClassB>();
            conn.CreateTable<O2OClassA>();
            conn.CreateTable<O2OClassB>();

            // Use standard SQLite-Net API to create a new relationship
            var objectB = new O2OClassB
            {
                Foo = string.Format("Foo String {0}", new Random().Next(100))
            };
            conn.Insert(objectB);

            var objectA = new O2OClassA();
            conn.Insert(objectA);

            // Set the relationship using objects
            objectA.OneClassB = objectB;
            Assert.AreEqual(0, objectA.OneClassBKey);

            conn.UpdateWithChildren(objectA);

            Assert.AreEqual(objectB.Id, objectA.OneClassBKey, "Foreign key should have been refreshed");

            // Until here, test is same that TestUpdateSetOneToOneRelationship
            objectA.OneClassB = null; // Unset relationship

            Assert.AreEqual(objectB.Id, objectA.OneClassBKey, "Foreign key shouldn't have been refreshed yet");

            conn.UpdateWithChildren(objectA);

            Assert.AreEqual(0, objectA.OneClassBKey, "Foreign key hasn't been unset");
        }

        [Test]
        public void TestUpdateSetOneToOneRelationshipWithInverse()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassE>();
            conn.DropTable<O2OClassF>();
            conn.CreateTable<O2OClassE>();
            conn.CreateTable<O2OClassF>();

            // Use standard SQLite-Net API to create a new relationship
            var objectF = new O2OClassF
            {
                Bar = string.Format("Bar String {0}", new Random().Next(100))
            };
            conn.Insert(objectF);

            var objectE = new O2OClassE();
            conn.Insert(objectE);

            // Set the relationship using objects
            objectE.ObjectF = objectF;
            Assert.AreEqual(0, objectE.ObjectFKey);

            conn.UpdateWithChildren(objectE);

            Assert.AreEqual(objectF.Id, objectE.ObjectFKey, "Foreign key should have been refreshed");
            Assert.AreSame(objectF, objectE.ObjectF, "Inverse relationship hasn't been set");

            // Fetch the relationship
            var newObjectA = conn.Get<O2OClassE>(objectE.Id);
            Assert.AreEqual(objectF.Id, newObjectA.ObjectFKey, "Foreign key should have been refreshed in database");
        }

        [Test]
        public void TestUpdateSetOneToOneRelationshipWithInverseForeignKey()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassF>();
            conn.DropTable<O2OClassE>();
            conn.CreateTable<O2OClassF>();
            conn.CreateTable<O2OClassE>();

            // Use standard SQLite-Net API to create a new relationship
            var objectF = new O2OClassF
            {
                Bar = string.Format("Bar String {0}", new Random().Next(100))
            };
            conn.Insert(objectF);

            var objectE = new O2OClassE();
            conn.Insert(objectE);

            // Set the relationship using objects
            objectF.ObjectE = objectE;
            Assert.AreEqual(0, objectE.ObjectFKey);

            conn.UpdateWithChildren(objectF);

            Assert.AreEqual(objectF.Id, objectE.ObjectFKey, "Foreign key should have been refreshed");
            Assert.AreSame(objectF, objectE.ObjectF, "Inverse relationship hasn't been set");

            // Fetch the relationship
            var newObjectA = conn.Get<O2OClassE>(objectE.Id);
            Assert.AreEqual(objectF.Id, newObjectA.ObjectFKey, "Foreign key should have been refreshed in database");
        }

        [Test]
        public void TestUpdateUnsetOneToOneRelationshipWithInverseForeignKey()
        {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassF>();
            conn.DropTable<O2OClassE>();
            conn.CreateTable<O2OClassF>();
            conn.CreateTable<O2OClassE>();

            // Use standard SQLite-Net API to create a new relationship
            var objectF = new O2OClassF
            {
                Bar = string.Format("Bar String {0}", new Random().Next(100))
            };
            conn.Insert(objectF);

            var objectE = new O2OClassE();
            conn.Insert(objectE);

            // Set the relationship using objects
            objectF.ObjectE = objectE;
            Assert.AreEqual(0, objectE.ObjectFKey);

            conn.UpdateWithChildren(objectF);

            Assert.AreEqual(objectF.Id, objectE.ObjectFKey, "Foreign key should have been refreshed");
            Assert.AreSame(objectF, objectE.ObjectF, "Inverse relationship hasn't been set");

            // At this point the test is the same as TestUpdateSetOneToOneRelationshipWithInverseForeignKey
            objectF.ObjectE = null;     // Unset the relationship

            conn.UpdateWithChildren(objectF);

            // Fetch the relationship
            var newObjectA = conn.Get<O2OClassE>(objectE.Id);
            Assert.AreEqual(0, newObjectA.ObjectFKey, "Foreign key should have been refreshed in database");
        }

        [Test]
        public void TestGetAllNoFilter() {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassA>();
            conn.DropTable<O2OClassB>();
            conn.CreateTable<O2OClassA>();
            conn.CreateTable<O2OClassB>();

            var a1 = new O2OClassA();
            var a2 = new O2OClassA();
            var a3 = new O2OClassA();
            var aObjects = new []{ a1, a2, a3 };
            conn.InsertAll(aObjects);

            var b1 = new O2OClassB{ Foo = "Foo 1" };
            var b2 = new O2OClassB{ Foo = "Foo 2" };
            var b3 = new O2OClassB{ Foo = "Foo 3" };
            var bObjects = new []{ b1, b2, b3 };
            conn.InsertAll(bObjects);

            a1.OneClassB = b1;
            a2.OneClassB = b2;
            a3.OneClassB = b3;
            conn.UpdateWithChildren(a1);
            conn.UpdateWithChildren(a2);
            conn.UpdateWithChildren(a3);

            var aElements = conn.GetAllWithChildren<O2OClassA>().OrderBy(a => a.Id).ToArray();
            Assert.AreEqual(aObjects.Length, aElements.Length);
            for (int i = 0; i < aObjects.Length; i++) {
                Assert.AreEqual(aObjects[i].Id, aElements[i].Id);
                Assert.AreEqual(aObjects[i].OneClassB.Id, aElements[i].OneClassB.Id);
                Assert.AreEqual(aObjects[i].OneClassB.Foo, aElements[i].OneClassB.Foo);
            }

        }

        [Test]
        public void TestGetAllFilter() {
            var conn = Utils.CreateConnection();
            conn.DropTable<O2OClassC>();
            conn.DropTable<O2OClassD>();
            conn.CreateTable<O2OClassC>();
            conn.CreateTable<O2OClassD>();

            var c1 = new O2OClassC { Bar = "Bar 1" };
            var c2 = new O2OClassC { Bar = "Foo 2" };
            var c3 = new O2OClassC { Bar = "Bar 3" };
            var cObjects = new []{ c1, c2, c3 };
            conn.InsertAll(cObjects);

            var d1 = new O2OClassD{ Foo = "Foo 1" };
            var d2 = new O2OClassD{ Foo = "Foo 2" };
            var d3 = new O2OClassD{ Foo = "Foo 3" };
            var bObjects = new []{ d1, d2, d3 };
            conn.InsertAll(bObjects);

            c1.ElementD = d1;
            c2.ElementD = d2;
            c3.ElementD = d3;
            conn.UpdateWithChildren(c1);
            conn.UpdateWithChildren(c2);
            conn.UpdateWithChildren(c3);

            var expectedCObjects = cObjects.Where(c => c.Bar.Contains("Bar")).ToArray();
            var cElements = conn.GetAllWithChildren<O2OClassC>(c => c.Bar.Contains("Bar"))
                .OrderBy(a => a.ClassId).ToArray();

            Assert.AreEqual(expectedCObjects.Length, cElements.Length);
            for (int i = 0; i < expectedCObjects.Length; i++) {
                Assert.AreEqual(expectedCObjects[i].ClassId, cElements[i].ClassId);
                Assert.AreEqual(expectedCObjects[i].ElementD.Id, cElements[i].ElementD.Id);
                Assert.AreEqual(expectedCObjects[i].ElementD.Foo, cElements[i].ElementD.Foo);
            }

        }
    }
}

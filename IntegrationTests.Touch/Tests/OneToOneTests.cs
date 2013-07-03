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

        public string Foo { get; set; }
    }

    public class ClassC
    {
        [PrimaryKey, AutoIncrement]
        public int ClassId { get; set; }

        [OneToOne]
        public ClassD ElementD { get; set; }

        public string Bar { get; set; }
    }

    public class ClassD
    {
        [ForeignKey(typeof (ClassC))]
        public int ObjectCKey { get; set; }

        public string Foo { get; set; }
    }

    [TestFixture]
    public class OneToOneTests
    {

        [Test]
        public void TestGetOneToOneDirect()
        {
            var conn = new SQLiteConnection("database");
            conn.DropTable<ClassA>();
            conn.DropTable<ClassB>();
            conn.CreateTable<ClassA>();
            conn.CreateTable<ClassB>();

            // Use standard SQLite-Net API to create a new relationship
            var objectB = new ClassB
                {
                    Foo = string.Format("Foo String {0}", new Random().Next(100))
                };
            conn.Insert(objectB);

            var objectA = new ClassA();
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

        [Test]
        public void TestGetOneToOneInverseForeignKey()
        {
            var conn = new SQLiteConnection("database");
            conn.DropTable<ClassC>();
            conn.DropTable<ClassD>();
            conn.CreateTable<ClassC>();
            conn.CreateTable<ClassD>();

            // Use standard SQLite-Net API to create a new relationship
            var objectC = new ClassC
            {
                Bar = string.Format("Bar String {0}", new Random().Next(100))
            };
            conn.Insert(objectC);

            Assert.Null(objectC.ElementD);

            // Fetch (yet empty) the relationship
            conn.GetChildren(ref objectC);

            Assert.Null(objectC.ElementD);

            var objectD = new ClassD
            {
                ObjectCKey = objectC.ClassId,
                Foo = string.Format("Foo String {0}", new Random().Next(100))
            };
            conn.Insert(objectD);

            Assert.Null(objectC.ElementD);

            // Fetch the relationship
            conn.GetChildren(ref objectC);

            Assert.NotNull(objectC.ElementD);
            Assert.AreEqual(objectC.ClassId, objectC.ElementD.ObjectCKey);
            Assert.AreEqual(objectD.Foo, objectC.ElementD.Foo);
        }
    }
}

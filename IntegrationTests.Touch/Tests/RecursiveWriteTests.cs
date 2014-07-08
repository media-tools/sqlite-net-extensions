using System;
using NUnit.Framework;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.IntegrationTests;
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
    public class RecursiveWriteTests
    {
        #region OneToOneRecursiveInsert
        public class Passport {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string PassportNumber { get; set; }

            [ForeignKey(typeof(Person))]
            public int OwnerId { get; set; }

            [OneToOne(CascadeOperations = CascadeOperation.CascadeInsert)]
            public Person Owner { get; set; }
        }

        public class Person {
            [PrimaryKey, AutoIncrement]
            public int Identifier { get; set; }

            public string Name { get; set; }
            public string Surname { get; set; }

            [OneToOne(CascadeOperations = CascadeOperation.CascadeInsert)]
            public Passport Passport { get; set; }
        }

        [Test]
        public void TestOneToOneRecursiveInsert() {
            var conn = Utils.CreateConnection();
            conn.DropTable<Passport>();
            conn.DropTable<Person>();
            conn.CreateTable<Passport>();
            conn.CreateTable<Person>();

            var person = new Person
            {
                Name = "John",
                Surname = "Smith",
                Passport = new Passport {
                    PassportNumber = "JS123456"
                }
            };

            // Insert the elements in the database recursively
            conn.InsertWithChildren(person, recursive: true);

            var obtainedPerson = conn.Find<Person>(person.Identifier);
            var obtainedPassport = conn.Find<Passport>(person.Passport.Id);

            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPassport);
            Assert.That(obtainedPerson.Name, Is.EqualTo(person.Name));
            Assert.That(obtainedPerson.Surname, Is.EqualTo(person.Surname));
            Assert.That(obtainedPassport.PassportNumber, Is.EqualTo(person.Passport.PassportNumber));
        }

        [Test]
        public void TestOneToOneRecursiveInsertOrReplace() {
            var conn = Utils.CreateConnection();
            conn.DropTable<Passport>();
            conn.DropTable<Person>();
            conn.CreateTable<Passport>();
            conn.CreateTable<Person>();

            var person = new Person
            {
                Name = "John",
                Surname = "Smith",
                Passport = new Passport {
                    PassportNumber = "JS123456"
                }
            };

            // Insert the elements in the database recursively
            conn.InsertOrReplaceWithChildren(person, recursive: true);

            var obtainedPerson = conn.Find<Person>(person.Identifier);
            var obtainedPassport = conn.Find<Passport>(person.Passport.Id);

            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPassport);
            Assert.That(obtainedPerson.Name, Is.EqualTo(person.Name));
            Assert.That(obtainedPerson.Surname, Is.EqualTo(person.Surname));
            Assert.That(obtainedPassport.PassportNumber, Is.EqualTo(person.Passport.PassportNumber));


            // Replace the elements in the database recursively
            conn.InsertOrReplaceWithChildren(person, recursive: true);

            obtainedPerson = conn.Find<Person>(person.Identifier);
            obtainedPassport = conn.Find<Passport>(person.Passport.Id);

            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPassport);
            Assert.That(obtainedPerson.Name, Is.EqualTo(person.Name));
            Assert.That(obtainedPerson.Surname, Is.EqualTo(person.Surname));
            Assert.That(obtainedPassport.PassportNumber, Is.EqualTo(person.Passport.PassportNumber));
        }
        #endregion

        #region OneToOneRecursiveInsertGuid
        public class PassportGuid {
            [PrimaryKey]
            public Guid Id { get; set; }

            public string PassportNumber { get; set; }

            [ForeignKey(typeof(PersonGuid))]
            public Guid OwnerId { get; set; }

            [OneToOne(CascadeOperations = CascadeOperation.CascadeInsert)]
            public PersonGuid Owner { get; set; }
        }

        public class PersonGuid {
            [PrimaryKey]
            public Guid Identifier { get; set; }

            public string Name { get; set; }
            public string Surname { get; set; }

            [OneToOne(CascadeOperations = CascadeOperation.CascadeInsert)]
            public PassportGuid Passport { get; set; }
        }

        [Test]
        public void TestOneToOneRecursiveInsertGuid() {
            var conn = Utils.CreateConnection();
            conn.DropTable<PassportGuid>();
            conn.DropTable<PersonGuid>();
            conn.CreateTable<PassportGuid>();
            conn.CreateTable<PersonGuid>();

            var person = new PersonGuid
            {
                Identifier = Guid.NewGuid(),
                Name = "John",
                Surname = "Smith",
                Passport = new PassportGuid {
                    Id = Guid.NewGuid(),
                    PassportNumber = "JS123456"
                }
            };

            // Insert the elements in the database recursively
            conn.InsertWithChildren(person, recursive: true);

            var obtainedPerson = conn.Find<PersonGuid>(person.Identifier);
            var obtainedPassport = conn.Find<PassportGuid>(person.Passport.Id);

            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPassport);
            Assert.That(obtainedPerson.Name, Is.EqualTo(person.Name));
            Assert.That(obtainedPerson.Surname, Is.EqualTo(person.Surname));
            Assert.That(obtainedPassport.PassportNumber, Is.EqualTo(person.Passport.PassportNumber));
        }

        [Test]
        public void TestOneToOneRecursiveInsertOrReplaceGuid() {
            var conn = Utils.CreateConnection();
            conn.DropTable<PassportGuid>();
            conn.DropTable<PersonGuid>();
            conn.CreateTable<PassportGuid>();
            conn.CreateTable<PersonGuid>();

            var person = new PersonGuid
            {
                Identifier = Guid.NewGuid(),
                Name = "John",
                Surname = "Smith",
                Passport = new PassportGuid {
                    Id = Guid.NewGuid(),
                    PassportNumber = "JS123456"
                }
            };

            // Insert the elements in the database recursively
            conn.InsertOrReplaceWithChildren(person, recursive: true);

            var obtainedPerson = conn.Find<PersonGuid>(person.Identifier);
            var obtainedPassport = conn.Find<PassportGuid>(person.Passport.Id);

            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPassport);
            Assert.That(obtainedPerson.Name, Is.EqualTo(person.Name));
            Assert.That(obtainedPerson.Surname, Is.EqualTo(person.Surname));
            Assert.That(obtainedPassport.PassportNumber, Is.EqualTo(person.Passport.PassportNumber));


            // Replace the elements in the database recursively
            conn.InsertOrReplaceWithChildren(person, recursive: true);

            obtainedPerson = conn.Find<PersonGuid>(person.Identifier);
            obtainedPassport = conn.Find<PassportGuid>(person.Passport.Id);

            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPassport);
            Assert.That(obtainedPerson.Name, Is.EqualTo(person.Name));
            Assert.That(obtainedPerson.Surname, Is.EqualTo(person.Surname));
            Assert.That(obtainedPassport.PassportNumber, Is.EqualTo(person.Passport.PassportNumber));
        }
        #endregion
    }
}


using System;
using NUnit.Framework;
using SQLiteNetExtensions.IntegrationTests;
using SQLite.Net.Attributes;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;

namespace IntegrationTests.Touch.Tests
{
    [TestFixture]
    public class RecursiveReadTests
    {
        #region TestOneToOneCascadeWithInverse
        public class PassportWithForeignKey {
            [PrimaryKey]
            public int Id { get; set; }

            public string PassportNumber { get; set; }

            [ForeignKey(typeof(PersonNoForeignKey))]
            public int OwnerId { get; set; }

            [OneToOne(CascadeOperations = CascadeOperation.CascadeRead)]
            public PersonNoForeignKey Owner { get; set; }
        }

        public class PersonNoForeignKey {
            [PrimaryKey]
            public int Identifier { get; set; }

            public string Name { get; set; }
            public string Surname { get; set; }

            [OneToOne(CascadeOperations = CascadeOperation.CascadeRead)]
            public PassportWithForeignKey Passport { get; set; }
        }

        [Test]
        public void TestOneToOneCascadeWithInverse() {
            var conn = Utils.CreateConnection();
            conn.DropTable<PassportWithForeignKey>();
            conn.DropTable<PersonNoForeignKey>();
            conn.CreateTable<PassportWithForeignKey>();
            conn.CreateTable<PersonNoForeignKey>();

            var person = new PersonNoForeignKey { Name = "John", Surname = "Smith" };
            conn.Insert(person);

            var passport = new PassportWithForeignKey { PassportNumber = "JS12345678", Owner = person };
            conn.Insert(passport);
            conn.UpdateWithChildren(passport);

            var obtainedPerson = conn.GetWithChildren<PersonNoForeignKey>(person.Identifier, recursive: true);
            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPerson.Passport);
            Assert.NotNull(obtainedPerson.Passport.Owner, "Circular reference should've been solved");
            Assert.AreEqual(obtainedPerson.Identifier, obtainedPerson.Passport.Owner.Identifier, "Integral reference check");
            Assert.AreEqual(obtainedPerson.Passport.Id, obtainedPerson.Passport.Owner.Passport.Id);
            Assert.AreEqual(person.Identifier, obtainedPerson.Identifier);
            Assert.AreEqual(passport.Id, obtainedPerson.Passport.Id);

            var obtainedPassport = conn.GetWithChildren<PassportWithForeignKey>(passport.Id, recursive: true);
            Assert.NotNull(obtainedPassport);
            Assert.NotNull(obtainedPassport.Owner);
            Assert.NotNull(obtainedPassport.Owner.Passport, "Circular reference should've been solved");
            Assert.AreEqual(obtainedPassport.Id, obtainedPassport.Owner.Passport.Id);
            Assert.AreEqual(obtainedPassport.Owner.Identifier, obtainedPassport.Owner.Passport.Owner.Identifier);
            Assert.AreEqual(passport.Id, obtainedPassport.Id);
            Assert.AreEqual(person.Identifier, obtainedPassport.Owner.Identifier);
        }

        /// <summary>
        /// Same test that TestOneToOneCascadeWithInverse but fetching the passport instead of the person
        /// </summary>
        [Test]
        public void TestOneToOneCascadeWithInverseReversed() {
            var conn = Utils.CreateConnection();
            conn.DropTable<PassportWithForeignKey>();
            conn.DropTable<PersonNoForeignKey>();
            conn.CreateTable<PassportWithForeignKey>();
            conn.CreateTable<PersonNoForeignKey>();

            var person = new PersonNoForeignKey { Name = "John", Surname = "Smith" };
            conn.Insert(person);

            var passport = new PassportWithForeignKey { PassportNumber = "JS12345678", Owner = person };
            conn.Insert(passport);
            conn.UpdateWithChildren(passport);

            var obtainedPassport = conn.GetWithChildren<PassportWithForeignKey>(passport.Id, recursive: true);
            Assert.NotNull(obtainedPassport);
            Assert.NotNull(obtainedPassport.Owner);
            Assert.NotNull(obtainedPassport.Owner.Passport, "Circular reference should've been solved");
            Assert.AreEqual(obtainedPassport.Id, obtainedPassport.Owner.Passport.Id);
            Assert.AreEqual(obtainedPassport.Owner.Identifier, obtainedPassport.Owner.Passport.Owner.Identifier);
            Assert.AreEqual(passport.Id, obtainedPassport.Id);
            Assert.AreEqual(person.Identifier, obtainedPassport.Owner.Identifier);
        }
        #endregion

        #region TestOneToOneCascadeWithInverseDoubleForeignKey
        public class PassportWithForeignKeyDouble {
            [PrimaryKey]
            public int Id { get; set; }

            public string PassportNumber { get; set; }

            [ForeignKey(typeof(PersonWithForeignKey))]
            public int OwnerId { get; set; }

            [OneToOne(CascadeOperations = CascadeOperation.CascadeRead)]
            public PersonWithForeignKey Owner { get; set; }
        }

        public class PersonWithForeignKey {
            [PrimaryKey]
            public int Identifier { get; set; }

            public string Name { get; set; }
            public string Surname { get; set; }

            [ForeignKey(typeof(PassportWithForeignKeyDouble))]
            public int PassportId { get; set; }

            [OneToOne(CascadeOperations = CascadeOperation.CascadeRead)]
            public PassportWithForeignKeyDouble Passport { get; set; }
        }

        [Test]
        public void TestOneToOneCascadeWithInverseDoubleForeignKey() {
            var conn = Utils.CreateConnection();
            conn.DropTable<PassportWithForeignKeyDouble>();
            conn.DropTable<PersonWithForeignKey>();
            conn.CreateTable<PassportWithForeignKeyDouble>();
            conn.CreateTable<PersonWithForeignKey>();

            var person = new PersonWithForeignKey { Name = "John", Surname = "Smith" };
            conn.Insert(person);

            var passport = new PassportWithForeignKeyDouble { PassportNumber = "JS12345678", Owner = person };
            conn.Insert(passport);
            conn.UpdateWithChildren(passport);

            var obtainedPerson = conn.GetWithChildren<PersonWithForeignKey>(person.Identifier, recursive: true);
            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPerson.Passport);
            Assert.NotNull(obtainedPerson.Passport.Owner, "Circular reference should've been solved");
            Assert.AreEqual(obtainedPerson.Identifier, obtainedPerson.Passport.Owner.Identifier, "Integral reference check");
            Assert.AreEqual(obtainedPerson.Passport.Id, obtainedPerson.Passport.Owner.Passport.Id);
            Assert.AreEqual(person.Identifier, obtainedPerson.Identifier);
            Assert.AreEqual(passport.Id, obtainedPerson.Passport.Id);
        }

        /// <summary>
        /// Same test that TestOneToOneCascadeWithInverseDoubleForeignKey but fetching the passport instead of the person
        /// </summary>
        [Test]
        public void TestOneToOneCascadeWithInverseDoubleForeignKeyReversed() {
            var conn = Utils.CreateConnection();
            conn.DropTable<PassportWithForeignKeyDouble>();
            conn.DropTable<PersonWithForeignKey>();
            conn.CreateTable<PassportWithForeignKeyDouble>();
            conn.CreateTable<PersonWithForeignKey>();

            var person = new PersonWithForeignKey { Name = "John", Surname = "Smith" };
            conn.Insert(person);

            var passport = new PassportWithForeignKeyDouble { PassportNumber = "JS12345678", Owner = person };
            conn.Insert(passport);
            conn.UpdateWithChildren(passport);

            var obtainedPassport = conn.GetWithChildren<PassportWithForeignKeyDouble>(passport.Id, recursive: true);
            Assert.NotNull(obtainedPassport);
            Assert.NotNull(obtainedPassport.Owner);
            Assert.NotNull(obtainedPassport.Owner.Passport, "Circular reference should've been solved");
            Assert.AreEqual(obtainedPassport.Id, obtainedPassport.Owner.Passport.Id);
            Assert.AreEqual(obtainedPassport.Owner.Identifier, obtainedPassport.Owner.Passport.Owner.Identifier);
            Assert.AreEqual(passport.Id, obtainedPassport.Id);
            Assert.AreEqual(person.Identifier, obtainedPassport.Owner.Identifier);
        }
        #endregion

    }
}


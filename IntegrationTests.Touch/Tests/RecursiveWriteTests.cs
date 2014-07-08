using System;
using NUnit.Framework;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.IntegrationTests;
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
    public class RecursiveWriteTests
    {
        #region OneToOneRecursiveInsert
        public class Person {
            [PrimaryKey, AutoIncrement]
            public int Identifier { get; set; }
            
            public string Name { get; set; }
            public string Surname { get; set; }
            
            [OneToOne(CascadeOperations = CascadeOperation.CascadeInsert)]
            public Passport Passport { get; set; }
        }

        public class Passport {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string PassportNumber { get; set; }

            [ForeignKey(typeof(Person))]
            public int OwnerId { get; set; }

            [OneToOne(ReadOnly = true)]
            public Person Owner { get; set; }
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
            Assert.That(obtainedPassport.OwnerId, Is.EqualTo(person.Identifier));
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
            Assert.That(obtainedPassport.OwnerId, Is.EqualTo(person.Identifier));


            var newPerson = new Person
            {
                Identifier = person.Identifier,
                Name = "John",
                Surname = "Smith",
                Passport = new Passport {
                    Id = person.Passport.Id,
                    PassportNumber = "JS123456"
                }
            };
            person = newPerson;

            // Replace the elements in the database recursively
            conn.InsertOrReplaceWithChildren(person, recursive: true);

            obtainedPerson = conn.Find<Person>(person.Identifier);
            obtainedPassport = conn.Find<Passport>(person.Passport.Id);

            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPassport);
            Assert.That(obtainedPerson.Name, Is.EqualTo(person.Name));
            Assert.That(obtainedPerson.Surname, Is.EqualTo(person.Surname));
            Assert.That(obtainedPassport.PassportNumber, Is.EqualTo(person.Passport.PassportNumber));
            Assert.That(obtainedPassport.OwnerId, Is.EqualTo(person.Identifier));
        }
        #endregion

        #region OneToOneRecursiveInsertGuid
        public class PersonGuid {
            [PrimaryKey]
            public Guid Identifier { get; set; }
            
            public string Name { get; set; }
            public string Surname { get; set; }
            
            [OneToOne(CascadeOperations = CascadeOperation.CascadeInsert)]
            public PassportGuid Passport { get; set; }
        }

        public class PassportGuid {
            [PrimaryKey]
            public Guid Id { get; set; }

            public string PassportNumber { get; set; }

            [ForeignKey(typeof(PersonGuid))]
            public Guid OwnerId { get; set; }

            [OneToOne(ReadOnly = true)]
            public PersonGuid Owner { get; set; }
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
            Assert.That(obtainedPassport.OwnerId, Is.EqualTo(person.Identifier));
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
            Assert.That(obtainedPassport.OwnerId, Is.EqualTo(person.Identifier));


            var newPerson = new PersonGuid
            {
                Identifier = person.Identifier,
                Name = "John",
                Surname = "Smith",
                Passport = new PassportGuid {
                    Id = person.Passport.Id,
                    PassportNumber = "JS123456"
                }
            };
            person = newPerson;

            // Replace the elements in the database recursively
            conn.InsertOrReplaceWithChildren(person, recursive: true);

            obtainedPerson = conn.Find<PersonGuid>(person.Identifier);
            obtainedPassport = conn.Find<PassportGuid>(person.Passport.Id);

            Assert.NotNull(obtainedPerson);
            Assert.NotNull(obtainedPassport);
            Assert.That(obtainedPerson.Name, Is.EqualTo(person.Name));
            Assert.That(obtainedPerson.Surname, Is.EqualTo(person.Surname));
            Assert.That(obtainedPassport.PassportNumber, Is.EqualTo(person.Passport.PassportNumber));
            Assert.That(obtainedPassport.OwnerId, Is.EqualTo(person.Identifier));
        }
        #endregion

        #region OneToManyRecursiveInsert
        public class Customer {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string Name { get; set; }

            [OneToMany(CascadeOperations = CascadeOperation.CascadeInsert)]
            public Order[] Orders { get; set; }
        }

        [Table("Orders")] // 'Order' is a reserved keyword
        public class Order {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public float Amount { get; set; }
            public DateTime Date { get; set; }

            [ForeignKey(typeof(Customer))]
            public int CustomerId { get; set; }

            [ManyToOne]
            public Customer Customer { get; set; }
        }

        [Test]
        public void TestOneToManyRecursiveInsert() {
            var conn = Utils.CreateConnection();
            conn.DropTable<Customer>();
            conn.DropTable<Order>();
            conn.CreateTable<Customer>();
            conn.CreateTable<Order>();

            var customer = new Customer
            { 
                Name = "John Smith",
                Orders = new []
                {
                    new Order { Amount = 25.7f, Date = new DateTime(2014, 5, 15, 11, 30, 15) },
                    new Order { Amount = 15.2f, Date = new DateTime(2014, 3, 7, 13, 59, 1) },
                    new Order { Amount = 0.5f, Date = new DateTime(2014, 4, 5, 7, 3, 0) },
                    new Order { Amount = 106.6f, Date = new DateTime(2014, 7, 20, 21, 20, 24) },
                    new Order { Amount = 98f, Date = new DateTime(2014, 02, 1, 22, 31, 7) }
                }
            };

            conn.InsertWithChildren(customer, recursive: true);

            var expectedOrders = customer.Orders.OrderBy(o => o.Date).ToDictionary(o => o.Id);

            var obtainedCustomer = conn.GetWithChildren<Customer>(customer.Id, recursive: true);
            Assert.NotNull(obtainedCustomer);
            Assert.NotNull(obtainedCustomer.Orders);
            Assert.AreEqual(expectedOrders.Count, obtainedCustomer.Orders.Length);

            foreach (var order in obtainedCustomer.Orders)
            {
                var expectedOrder = expectedOrders[order.Id];
                Assert.AreEqual(expectedOrder.Amount, order.Amount, 0.0001);
                Assert.AreEqual(expectedOrder.Date, order.Date);
                Assert.NotNull(order.Customer);
                Assert.AreEqual(customer.Id, order.CustomerId);
                Assert.AreEqual(customer.Id, order.Customer.Id);
                Assert.AreEqual(customer.Name, order.Customer.Name);
                Assert.NotNull(order.Customer.Orders);
                Assert.AreEqual(expectedOrders.Count, order.Customer.Orders.Length);
            }
        }

        [Test]
        public void TestOneToManyRecursiveInsertOrReplace() {
            var conn = Utils.CreateConnection();
            conn.DropTable<Customer>();
            conn.DropTable<Order>();
            conn.CreateTable<Customer>();
            conn.CreateTable<Order>();

            var customer = new Customer
            { 
                Name = "John Smith",
                Orders = new []
                {
                    new Order { Amount = 25.7f, Date = new DateTime(2014, 5, 15, 11, 30, 15) },
                    new Order { Amount = 15.2f, Date = new DateTime(2014, 3, 7, 13, 59, 1) },
                    new Order { Amount = 0.5f, Date = new DateTime(2014, 4, 5, 7, 3, 0) },
                    new Order { Amount = 106.6f, Date = new DateTime(2014, 7, 20, 21, 20, 24) },
                    new Order { Amount = 98f, Date = new DateTime(2014, 02, 1, 22, 31, 7) }
                }
            };

            conn.InsertOrReplaceWithChildren(customer);

            var expectedOrders = customer.Orders.OrderBy(o => o.Date).ToDictionary(o => o.Id);

            var obtainedCustomer = conn.GetWithChildren<Customer>(customer.Id, recursive: true);
            Assert.NotNull(obtainedCustomer);
            Assert.NotNull(obtainedCustomer.Orders);
            Assert.AreEqual(expectedOrders.Count, obtainedCustomer.Orders.Length);

            foreach (var order in obtainedCustomer.Orders)
            {
                var expectedOrder = expectedOrders[order.Id];
                Assert.AreEqual(expectedOrder.Amount, order.Amount, 0.0001);
                Assert.AreEqual(expectedOrder.Date, order.Date);
                Assert.NotNull(order.Customer);
                Assert.AreEqual(customer.Id, order.CustomerId);
                Assert.AreEqual(customer.Id, order.Customer.Id);
                Assert.AreEqual(customer.Name, order.Customer.Name);
                Assert.NotNull(order.Customer.Orders);
                Assert.AreEqual(expectedOrders.Count, order.Customer.Orders.Length);
            }

            var newCustomer = new Customer
            { 
                Id = customer.Id,
                Name = "John Smith",
                Orders = new []
                {
                    new Order { Id = customer.Orders[0].Id, Amount = 15.7f, Date = new DateTime(2012, 5, 15, 11, 30, 15) },
                    new Order { Id = customer.Orders[2].Id, Amount = 55.2f, Date = new DateTime(2012, 3, 7, 13, 59, 1) },
                    new Order { Id = customer.Orders[4].Id, Amount = 4.5f, Date = new DateTime(2012, 4, 5, 7, 3, 0) },
                    new Order { Amount = 206.6f, Date = new DateTime(2012, 7, 20, 21, 20, 24) },
                    new Order { Amount = 78f, Date = new DateTime(2012, 02, 1, 22, 31, 7) }
                }
            };

            customer = newCustomer;

            conn.InsertOrReplaceWithChildren(customer, recursive: true);

            expectedOrders = customer.Orders.OrderBy(o => o.Date).ToDictionary(o => o.Id);

            obtainedCustomer = conn.GetWithChildren<Customer>(customer.Id, recursive: true);
            Assert.NotNull(obtainedCustomer);
            Assert.NotNull(obtainedCustomer.Orders);
            Assert.AreEqual(expectedOrders.Count, obtainedCustomer.Orders.Length);

            foreach (var order in obtainedCustomer.Orders)
            {
                var expectedOrder = expectedOrders[order.Id];
                Assert.AreEqual(expectedOrder.Amount, order.Amount, 0.0001);
                Assert.AreEqual(expectedOrder.Date, order.Date);
                Assert.NotNull(order.Customer);
                Assert.AreEqual(customer.Id, order.CustomerId);
                Assert.AreEqual(customer.Id, order.Customer.Id);
                Assert.AreEqual(customer.Name, order.Customer.Name);
                Assert.NotNull(order.Customer.Orders);
                Assert.AreEqual(expectedOrders.Count, order.Customer.Orders.Length);
            }
        }
        #endregion

        #region OneToManyRecursiveInsertGuid
        public class CustomerGuid {
            [PrimaryKey]
            public Guid Id { get; set; }

            public string Name { get; set; }

            [OneToMany(CascadeOperations = CascadeOperation.CascadeInsert)]
            public OrderGuid[] Orders { get; set; }
        }

        [Table("Orders")] // 'Order' is a reserved keyword
        public class OrderGuid {
            [PrimaryKey]
            public Guid Id { get; set; }

            public float Amount { get; set; }
            public DateTime Date { get; set; }

            [ForeignKey(typeof(CustomerGuid))]
            public Guid CustomerId { get; set; }

            [ManyToOne]
            public CustomerGuid Customer { get; set; }
        }

        [Test]
        public void TestOneToManyRecursiveInsertGuid() {
            var conn = Utils.CreateConnection();
            conn.DropTable<CustomerGuid>();
            conn.DropTable<OrderGuid>();
            conn.CreateTable<CustomerGuid>();
            conn.CreateTable<OrderGuid>();

            var customer = new CustomerGuid
            { 
                Id = Guid.NewGuid(),
                Name = "John Smith",
                Orders = new []
                {
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 25.7f, Date = new DateTime(2014, 5, 15, 11, 30, 15) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 15.2f, Date = new DateTime(2014, 3, 7, 13, 59, 1) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 0.5f, Date = new DateTime(2014, 4, 5, 7, 3, 0) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 106.6f, Date = new DateTime(2014, 7, 20, 21, 20, 24) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 98f, Date = new DateTime(2014, 02, 1, 22, 31, 7) }
                }
            };

            conn.InsertWithChildren(customer, recursive: true);

            var expectedOrders = customer.Orders.OrderBy(o => o.Date).ToDictionary(o => o.Id);

            var obtainedCustomer = conn.GetWithChildren<CustomerGuid>(customer.Id, recursive: true);
            Assert.NotNull(obtainedCustomer);
            Assert.NotNull(obtainedCustomer.Orders);
            Assert.AreEqual(expectedOrders.Count, obtainedCustomer.Orders.Length);

            foreach (var order in obtainedCustomer.Orders)
            {
                var expectedOrder = expectedOrders[order.Id];
                Assert.AreEqual(expectedOrder.Amount, order.Amount, 0.0001);
                Assert.AreEqual(expectedOrder.Date, order.Date);
                Assert.NotNull(order.Customer);
                Assert.AreEqual(customer.Id, order.CustomerId);
                Assert.AreEqual(customer.Id, order.Customer.Id);
                Assert.AreEqual(customer.Name, order.Customer.Name);
                Assert.NotNull(order.Customer.Orders);
                Assert.AreEqual(expectedOrders.Count, order.Customer.Orders.Length);
            }
        }

        [Test]
        public void TestOneToManyRecursiveInsertOrReplaceGuid() {
            var conn = Utils.CreateConnection();
            conn.DropTable<CustomerGuid>();
            conn.DropTable<OrderGuid>();
            conn.CreateTable<CustomerGuid>();
            conn.CreateTable<OrderGuid>();

            var customer = new CustomerGuid
            { 
                Id = Guid.NewGuid(),
                Name = "John Smith",
                Orders = new []
                {
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 25.7f, Date = new DateTime(2014, 5, 15, 11, 30, 15) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 15.2f, Date = new DateTime(2014, 3, 7, 13, 59, 1) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 0.5f, Date = new DateTime(2014, 4, 5, 7, 3, 0) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 106.6f, Date = new DateTime(2014, 7, 20, 21, 20, 24) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 98f, Date = new DateTime(2014, 02, 1, 22, 31, 7) }
                }
            };

            conn.InsertOrReplaceWithChildren(customer, recursive: true);

            var expectedOrders = customer.Orders.OrderBy(o => o.Date).ToDictionary(o => o.Id);

            var obtainedCustomer = conn.GetWithChildren<CustomerGuid>(customer.Id, recursive: true);
            Assert.NotNull(obtainedCustomer);
            Assert.NotNull(obtainedCustomer.Orders);
            Assert.AreEqual(expectedOrders.Count, obtainedCustomer.Orders.Length);

            foreach (var order in obtainedCustomer.Orders)
            {
                var expectedOrder = expectedOrders[order.Id];
                Assert.AreEqual(expectedOrder.Amount, order.Amount, 0.0001);
                Assert.AreEqual(expectedOrder.Date, order.Date);
                Assert.NotNull(order.Customer);
                Assert.AreEqual(customer.Id, order.CustomerId);
                Assert.AreEqual(customer.Id, order.Customer.Id);
                Assert.AreEqual(customer.Name, order.Customer.Name);
                Assert.NotNull(order.Customer.Orders);
                Assert.AreEqual(expectedOrders.Count, order.Customer.Orders.Length);
            }

            var newCustomer = new CustomerGuid
            { 
                Id = customer.Id,
                Name = "John Smith",
                Orders = new []
                {
                    new OrderGuid { Id = customer.Orders[0].Id, Amount = 15.7f, Date = new DateTime(2012, 5, 15, 11, 30, 15) },
                    new OrderGuid { Id = customer.Orders[2].Id, Amount = 55.2f, Date = new DateTime(2012, 3, 7, 13, 59, 1) },
                    new OrderGuid { Id = customer.Orders[4].Id, Amount = 4.5f, Date = new DateTime(2012, 4, 5, 7, 3, 0) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 206.6f, Date = new DateTime(2012, 7, 20, 21, 20, 24) },
                    new OrderGuid { Id = Guid.NewGuid(), Amount = 78f, Date = new DateTime(2012, 02, 1, 22, 31, 7) }
                }
            };

            customer = newCustomer;

            conn.InsertOrReplaceWithChildren(customer, recursive: true);

            expectedOrders = customer.Orders.OrderBy(o => o.Date).ToDictionary(o => o.Id);

            obtainedCustomer = conn.GetWithChildren<CustomerGuid>(customer.Id, recursive: true);
            Assert.NotNull(obtainedCustomer);
            Assert.NotNull(obtainedCustomer.Orders);
            Assert.AreEqual(expectedOrders.Count, obtainedCustomer.Orders.Length);

            foreach (var order in obtainedCustomer.Orders)
            {
                var expectedOrder = expectedOrders[order.Id];
                Assert.AreEqual(expectedOrder.Amount, order.Amount, 0.0001);
                Assert.AreEqual(expectedOrder.Date, order.Date);
                Assert.NotNull(order.Customer);
                Assert.AreEqual(customer.Id, order.CustomerId);
                Assert.AreEqual(customer.Id, order.Customer.Id);
                Assert.AreEqual(customer.Name, order.Customer.Name);
                Assert.NotNull(order.Customer.Orders);
                Assert.AreEqual(expectedOrders.Count, order.Customer.Orders.Length);
            }
        }
        #endregion
    }
}


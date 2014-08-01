# SQLite-Net Extensions

[SQLite-Net Extensions](https://bitbucket.org/twincoders/sqlite-net-extensions) is a very simple ORM that provides **one-to-one**, **one-to-many**, **many-to-one**, **many-to-many**, **inverse** and **text-blobbed** relationships on top of the [sqlite-net library](https://github.com/praeclarum/sqlite-net).

sqlite-net is an open source, minimal library to allow .NET and Mono applications to store data in [SQLite 3 databases](http://www.sqlite.org). SQLite-Net Extensions extends its funcionality to help the user handle relationships between sqlite-net entities.


## How it works
SQLite-Net Extensions provides attributes for specifying the relationships in different ways and uses reflection to read and write the objects at Runtime.

SQLite-Net Extensions doesn't create any table or columns in your database and doesn't persist obscure elements to handle the database. For this reason, you have full control over the database schema used to persist your entities. SQLite-Net Extensions only requires you to specify the foreign keys used to handle the relationships and it will find out the rest by itself.

SQLite-Net Extensions doesn't modify or override any method behavior of SQLite.Net. Instead, it extends `SQLiteConnection` class with methods to handle the relationships.

For example `GetChildren` finds all the relationship properties that have been specified in the element, finds the required foreign keys and fills the properties automatically for you.

Complementarily `UpdateWithChildren` looks at the relationships that you have set, updates all the foreign keys and save the changes in the database. This is particulary helpful for one-to-many, many-to-many or one-to-one relationships where the foreign key is in the destination table.

You can update foreign keys manually if you feel more comfortable handling some relationships by yourself and let the SQLite-Net extensions handle the rest for you. You can even add or remove SQLite-Net extensions of any project at any time without changes to your database.

## Installation
The easiest way of installing the library in your project is adding a reference to [_SQLiteNetExtensions_ NuGet package](https://www.nuget.org/packages/SQLiteNetExtensions/).

SQLite-Net Extensions is provided in three different flavours, depending on the SQLite-Net version that you are using:

- [SQLite-Net PCL](https://github.com/oysteinkrog/SQLite.Net-PCL) version (also as [NuGet package](https://www.nuget.org/packages/SQLite.Net-PCL/))
- [MvvmCross SQLite Community](https://github.com/MvvmCross/MvvmCross-SQLite PCL) version (also as [NuGet package](https://www.nuget.org/packages/MvvmCross.Community.Plugin.Sqlite/))
- [SQLite-Net standard](https://github.com/praeclarum/sqlite-net) version (also as [NuGet package](https://www.nuget.org/packages/sqlite-net))

Currently the recommended version is the official SQLite-Net PCL NuGet package. If you are using this SQLite-Net version, you can simply add a reference to [_SQLiteNetExtensions_ NuGet package](https://www.nuget.org/packages/SQLiteNetExtensions/).

There's also [a PCL NuGet package](https://www.nuget.org/packages/SQLiteNetExtensions-MvvmCross/) for users of MvvmCross SQLite Community plugin.

Otherwise, you can download and compile the sources by yourself and add the reference to your newly compiled DLL or add SQLite-Net Extensions project as a dependency to your code.

## Get help
The best way to get help is searching [StackOverflow](http://stackoverflow.com) for already existing answers of your problem or asking your own question and tagging it with [`sqlite-net-extensions` tag](http://stackoverflow.com/questions/tagged/sqlite-net-extensions).

If you find a bug or have a suggestion or feature request. Feel free to create a new ticket in the [issue tracker](https://bitbucket.org/twincoders/sqlite-net-extensions/issues?status=new&status=open) if it doesn't already exist.


## Some code
Theories are for the conspiracists, lets see some code.

#### sqlite-net version
This is how you usually specify a relationship in **sqlite-net** (extracted from [sqlite-net wiki](https://github.com/praeclarum/sqlite-net/wiki/GettingStarted)):

    public class Stock    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        [MaxLength(8)]        public string Symbol { get; set; }    }    public class Valuation    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        [Indexed]        public int StockId { get; set; }        public DateTime Time { get; set; }        public decimal Price { get; set; }    }

Then you obtain the Valuations for a specific Stock query like this:

    return db.Query<Valuation> ("select * from Valuation where StockId = ?", stock.Id);
    
#### SQLite-Net Extensions version

With SQLite-Net extensions, no more need to write the queries manually, just specify the relationships in the entities:

    public class Stock    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        [MaxLength(8)]        public string Symbol { get; set; }        [OneToMany(CascadeOperations = CascadeOperation.All)]      // One to many relationship with Valuation        public List<Valuation> Valuations { get; set; }    }    public class Valuation    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        [ForeignKey(typeof(Stock))]     // Specify the foreign key        public int StockId { get; set; }        public DateTime Time { get; set; }        public decimal Price { get; set; }        [ManyToOne]      // Many to one relationship with Stock        public Stock Stock { get; set; }    }

SQLite-Net Extensions will find all the properties with a relationship attribute and then find the foreign keys and inverse attributes with the matching type for them.

Note that `Stock.Valuations` property is a `OneToMany` relationship to `Valuation`, that already has a `ManyToOne` relationship to `Stock` in `Valuation.Stock` property. These inverse relationships and the `ForeignKey` property will be discovered and updated automatically at runtime.

#### Read and write operations
Here's how we'll create, read and update the entities:

    var db = Utils.CreateConnection();    db.CreateTable<Stock>();    db.CreateTable<Valuation>();    var euro = new Stock() {        Symbol = "€"    };    db.Insert(euro);   // Insert the object in the database    var valuation = new Valuation() {        Price = 15,        Time = DateTime.Now,    };    db.Insert(valuation);   // Insert the object in the database    // Objects created, let's stablish the relationship    euro.Valuations = new List<Valuation> { valuation };    db.UpdateWithChildren(euro);   // Update the changes into the database    if (valuation.Stock == euro) {        Debug.WriteLine("Inverse relationship already set, yay!");    }    // Get the object and the relationships    var storedValuation = db.GetWithChildren<Valuation>(valuation.Id);    if (euro.Symbol.Equals(storedValuation.Stock.Symbol)) {        Debug.WriteLine("Object and relationships loaded correctly!");    }
        
We've specified `AutoIncrement` primary keys, so we have to insert the objects to the database first to be assigned a correct primary key before stablishing the relationships.

##### Using recursive operations
The complexity of the sample can be reduced using recursive operations, that are explained in detail below. As we can see in the `Stock` class defined above, we've already specified cascade operations for the `Valuations` property, so we can use cascade operations to make the previous method easier as we don't need to manually insert the objects one by one in the database:

    var db = Utils.CreateConnection();
    db.CreateTable<Stock>();
    db.CreateTable<Valuation>();

    var valuation = new Valuation {
        Price = 15,
        Time = DateTime.Now,
    };

    var euro = new Stock {
        Symbol = "€",
        Valuations = new List<Valuation> { valuation }
    };

    db.InsertWithChildren(euro);   // Insert the object in the database

    if (valuation.Stock == euro) {
        Debug.WriteLine("Inverse relationship already set, yay!");
    }

    // Get the object and the relationships
    var storedValuation = db.GetWithChildren<Valuation>(valuation.Id);
    if (euro.Symbol.Equals(storedValuation.Stock.Symbol)) {
        Debug.WriteLine("Object and relationships loaded correctly!");
    }
    
Recursive operations are explained in detail in the _Cascade operations_ section.


## Features
SQLite-Net extensions is built on top of [sqlite-net library](https://github.com/praeclarum/sqlite-net), so obviously all the [features of **sqlite-net**](https://github.com/praeclarum/sqlite-net/wiki) are present in it.

If you don't know why a foreign key or an intermediate table is required at some point or what a relationship represents, take a look at [this article](http://www.onlamp.com/pub/a/onlamp/2001/03/20/aboutSQL.html) that explains pretty simple how relationships are stored in a database.

### One to one
The foreign key for a one-to-one relationship may be defined in any entity or even in **both** entities. In the latter case SQLite-Net extensions will automatically update inverse foreign keys when needed.

The **inverse** relationship for a **one-to-one** property is also a **one-to-one** relationship. SQLite-Net extensions will automatically load one-to-one inverse relationships, because the object is already loaded into memory and it has no DB overhead.

Example:

    public class Passport    {        [PrimaryKey]        public string Identifier { get; set; }        public DateTime ExpirationDate { get; set; }    }    public class Person    {
        [PrimaryKey, AutoIncrement]        public int Id { get; set; }                public string Name { get; set; }        [ForeignKey(typeof(Passport))]        public string PassportId { get; set; }        [OneToOne]        public Passport Passport { get; set; }    }

### One to many
The foreign key for a one-to-many relationship must be defined in the *many* end of the relationship.

The **inverse** relationship for a **one-to-many** property is a **many-to-one** relationship. SQLite-Net extensions will automatically load one-to-many inverse relationships, because the object is already loaded into memory and it has no DB overhead.

One-to-many relationships currently support `List` and `Array` of entities. They might be used indistinctly.

Order of the elements is not guarranteed and should be considered as totally random. If sorting is required it should be performed after the elements are loaded.

Example:

    public class Bus    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        public string PlateNumber { get; set; }        [OneToMany]        public List<Person> Passengers { get; set; }    }    public class Person    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        public string Name { get; set; }        [ForeignKey(typeof(Bus))]        public int BusId { get; set; }    }

### Many to one
Many-to-one is the opposite to a one-to-many relationship. They represent exactly the same relationship seen from opposite entities. It can also be seen as a one-to-one relationship with no inverse restrictions.

The foreign key for a many-to-one relationship must be defined in the *many* end of the relationship.

The **inverse** relationship for a **many-to-one** property is a **one-to-many** relationship. SQLite-Net extensions will not automatically load many-to-one inverse relationships.

Example:

    public class Bus    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        public string PlateNumber { get; set; }    }    public class Person    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        public string Name { get; set; }        [ForeignKey(typeof(Bus))]        public int BusId { get; set; }        [ManyToOne]        public Bus Bus { get; set; }    }


### Many to many
Many-to-many relationships cannot be expressed using a foreign key in one of the entities, because foreign keys represent X-to-one relationships. Instead, an intermediate entity is required. This entity is never used directly in the application, but for clarity's shake, SQLite-Net Extensions will never create a table that the user hasn't defined explicitly.

The foreign keys for a many-to-many relationship are thus declared in a intermediate entity.

The **inverse** relationship for a **many-to-many** property is a **many-to-many** relationship. SQLite-Net extensions will not automatically load many-to-many inverse relationships.

Example:

    public class Student    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        public string Name { get; set; }        [ManyToMany(typeof(StudentSubject))]        public List<Student> Students { get; set; }     }    public class Subject    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        public string Description { get; set; }        [ManyToMany(typeof(StudentSubject))]        public List<Subject> Subjects { get; set; }     }    public class StudentSubject    {        [ForeignKey(typeof(Student))]        public int StudentId { get; set; }        [ForeignKey(typeof(Subject))]        public int SubjectId { get; set; }    }

### Inverse relationships
Inverse relationship are automatically discovered on runtime using reflection by matching the type of the origin entity with the type of the relationship of the opposite entity.

You may also explicitly declare the inverse property in the relationship attribute. This is required when more than one relationship with the same type is declared.

Example:

    public class Bus    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        public string PlateNumber { get; set; }        [OneToMany]        public List<Person> Passengers { get; set; }    }    public class Person    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        public string Name { get; set; }        [ForeignKey(typeof(Bus))]        public int BusId { get; set; }        [ManyToOne]        public Bus Bus { get; set; }    }

### Text blobbed properties
Text-blobbed properties are serialized into a text property when saved and deserialized when loaded. This allows storing simple objects in the same table in a single column.

Text-blobbed properties have a small overhead of serializing and deserializing the objects and some limitations, but are the best way to store simple objects like `List` or `Dictionary` of basic types or simple relationships.

Text-blobbed properties require a declared `string` property where the serialized object is stored.

The serializer used to store and load the elements can be customized by implementing the simple `ITextBlobSerializer` interface:

    public interface ITextBlobSerializer    {        string Serialize(object element);        object Deserialize(string text, Type type);    }

A JSON-based serializer is used if no other serializer has been specified using `TextBlobOperations.SetTextSerializer` method. To use the JSON serializer, a reference to [Newtonsoft Json.Net library](http://james.newtonking.com/projects/json-net.aspx) must be included in the project, also available as a [NuGet package](http://www.nuget.org/packages/newtonsoft.json/).

Text-blobbed properties cannot have relationships to other objects nor inverse relationship to its parent.

Example:

    public class Address    {        public string StreetName { get; set; }        public string Number { get; set; }        public string PostalCode { get; set; }        public string Country { get; set; }    }    public class Person    {        public string Name { get; set; }        [TextBlob("PhonesBlobbed")]        public List<string> PhoneNumbers { get; set; }        [TextBlob("AddressesBlobbed")]        public List<Address> Addresses { get; set; }         public string PhonesBlobbed { get; set; } // serialized phone numbers        public string AddressesBlobbed { get; set; } // serialized addresses    }
    
### Cascade operations
For safety, all operations are not recursive by default, but most of them can be configured to work recursively. SQLite-Net Extensions provides mechanisms for handling inverse relationships, references to the same class and circular dependencies out-of-the-box, so you won't have to worry about it. To handle it, there's a new property in all relationship attributes called `CascadeOperations` that allows you to specify how that relationship should behave on cascade operations. Cascade operations can be combined using the binary _OR_ operator `|`, for example:
	[OneToMany(CascadeOperations = CascadeOperation.CascadeRead | CascadeOperation.CascadeInsert)]
#### Cascade read
Cascade read operations allow you to fetch a complete relationship tree from the database starting at the object that you are fetching and continuing with all the relationships with `CascadeOperations` set to `CascadeRead`. To perform a fetch recursively, just set the optional `recursive` parameter of any read method to `true`:

	public class Customer {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.CascadeRead)]
        public Order[] Orders { get; set; }
    }

	...
	    conn.GetWithChildren<Customer>(identifier, recursive: true);_Sample extracted from `RecursiveReadTests` and `RecursiveWriteTests` test classes_SQLite-Net Extensions will ensure that any object is loaded only once from the database and will resolve circular dependencies and inverse relationships while maintaining integral reference. This means that any returned object of the same class with the same identifier will be a reference to exactly the same object.
#### Cascade insertCascade insert operations allows to you insert or replace objects recursively in the database without having to worry about inserting them before assigning the relationships and so on. To enable recursive insert, make sure that the relationship properties that you want to insert recursively have `CascadeOperations` set to `CascadeInsert`.

Four different methods are used for recursively insert objects into the database: `InsertWithChildren`, `InsertOrReplaceWithChildren`, `InsertAllWithChildren` and `InsertOrReplaceAllWithChildren`, depending on the operation to be performed (_insert_ or _insert-or-replace_) and the number of elements (_one_ or _more_).

SQLite-Net Extensions will not try to replace elements in the database which primary key is auto-incremental and hasn't been set yet. This will ensure that `InsertOrReplace` variant methods work as expected with initial inserts with auto-incremental primary keys.

SQLite-Net Extensions will ensure that any object in the relationship tree is inserted once and only once. This makes this method work flawlessly with circular dependencies or inverse relationships.For example, using the same sample, we can change the `CascadeOperations` parameter to allow recursive insert:

	public class Customer {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.CascadeInsert)]
        public Order[] Orders { get; set; }
    }

	...
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

    conn.InsertWithChildren(customer, recursive: true);_Sample extracted from `RecursiveReadTests` and `RecursiveWriteTests` test classes_

Take into account that using `InsertOrReplaceWithChildren` to update a entity tree may impact performance as it will delete and re-insert all objects into the database regardless if they have been modified or not. It's recommended to keep track of the modified objects by yourself and update the objects calling `UpdateWithChildren` rather than calling `InsertOrReplaceWithChildren` to replace the object tree.#### Cascade deleteCascade delete operations allow you to delete a complete entity tree by just performing a simple operation. To enable recursive delete on a relationship property you have to set `CascadeOperations` property to `CascadeDelete` on that relationship attribute.
Two different methods are provided for recursive deletion: `DeleteAll` and `Delete`. These methods already exist in vanilly SQLite-Net, just make sure to call the overloaded method with the `recursive` parameter set to `true`. `DeleteAll` is just a convenience method for deleting a list of objects, but it's recommended over iterating on `Delete` because it will make sure that the objects are only deleted once and will perform the deletion on a single SQL `delete` statement.
SQLite-Net Extensions will handle circular references and inverse relationships correctly, and will only perform a `delete` statement for each class type to be deleted.
For example, using the same sample, we can change the `CascadeOperations` parameter to allow recursive delete:

	public class Customer {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        [OneToMany(CascadeOperations = CascadeOperation.CascadeDelete)]
        public Order[] Orders { get; set; }
    }

	...
    var customer = conn.Get<Customer>(1234);
    // All orders for this customer will be deleted from the database
    conn.Delete(customer, recursive: true);_Sample extracted from `RecursiveReadTests` and `RecursiveWriteTests` test classes_### Read only properties
Sometimes you just want a property to be loaded from database but you will never assign that property manually and you don't want it to be taken into account. This is when a `ReadOnly` property comes handy. By setting a relationship attribute `ReadOnly` flag, that property will only be read from the database and will be ignored for all insert or delete operations. This is particularly useful for handling inverse properties of `ManyToOne` or `ManyToMany`.
For example, if you're implementing a Twitter client, you'll probably add followers to a user, but you'll never modify the inverse relationship manually:
    public class TwitterUser {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }

        [ManyToMany(typeof(FollowerLeaderRelationshipTable), "LeaderId", "Followers",
            CascadeOperations = CascadeOperation.CascadeRead)]
        public List<TwitterUser> FollowingUsers { get; set; }

        // ReadOnly is required because we're not specifying the followers manually, but want to obtain them from database
        [ManyToMany(typeof(FollowerLeaderRelationshipTable), "FollowerId", "FollowingUsers",
            CascadeOperations = CascadeOperation.CascadeRead, ReadOnly = true)]
        public List<TwitterUser> Followers { get; set; }
    }

    // Intermediate class, not used directly anywhere in the code, only in ManyToMany attributes and table creation
    public class FollowerLeaderRelationshipTable {
        public int LeaderId { get; set; }
        public int FollowerId { get; set; }
    }_Sample extracted from `RecursiveReadTests` and `RecursiveWriteTests` test classes_
### Foreign keys
Foreign keys for a relationship are discovered on runtime using reflection matching the type of the relationship with the type specified in the `ForeignKey` attribute. 

You may also explicitly declare the foreign key in the relationship attribute. Explicitly declaring the foreign key is required when more than one relationship with the same type is declared.

Foreign key must have the same type as the identifier of the entity that it's referencing.

## Limitations
SQLite-Net it's not a fully featured ORM and because of its conception and implementation, it has some limitations:

#### Only objects with ID can be used in a relationship
If you use `[AutoIncrement]` in your *Primary Key* this also means that you need to insert the object in the database first to be assigned a primary key.

Relationships are based in *foreign keys*. Foreign keys are just a reference to the primary key of another table. If the referenced object doesn't have an assigned primary key, there's no way to store this relationship in the database. We could *dectect* somehow if the object hasn't been persisted yet and then insert it to the database, but we would be losing control and again that is one of our main principles.

Before calling `UpdateWithChildren` make sure you have inserted all referenced objects in the database and you will be fine.

Using recursive insert operations will take this limitation into accound and it will handle it by itself.

#### Inverse relationships are not loaded automatically on many-to-* relationships
Because of *many-to-many* and *many-to-one* relationships inverse may reference to **a lot** of objects, they have to be loaded manually using `GetChildren` method. Only *one-to-one* and *one-to-many* inverse relationships will be loaded automatically. If you want to load *to_many* relationships automatically, you can use the cascade operations to load the entity tree recursively.

#### Foreign keys are not updated automatically for removed references
When you call to `UpdateWithChildren` method, it refreshes all the foreign keys based on the current relationships (including the inverse relationships), and stores the changes in the database. But if you remove a reference to an object, there's no way for SQLite-Net Extensions to know that you have an object in memory that wasn't referenced before. If you later call to `UpdateWithChildren` to the referenced object, you may find that the removed reference is unintentionally restored.

To keep a reference to the removed object it's recommended to reload the referenced object from the database.

## I want [moar](http://slappersonly.com/wordpress/wp-content/uploads/2013/02/moar.jpg)!!
I'd really recommend you to take a look at the Integration Tests project as it covers most of the use cases and contains lots of code. They are packed into a Xamarin.iOS project using NUnit Lite. Run it as a normal Xamarin.iOS App to execute the tests.

> Those are my principles. If you don't like them I have others.


We'd like to hear from you. Don't hesitate in contacting us for suggestions, feature requests, bug reports or anything you need.

## License

Copyright (C) 2013 TwinCoders S.L.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

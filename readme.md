### SQLite-Net Extensions

[SQLite-Net Extensions](https://bitbucket.org/twincoders/sqlite-net-extensions) is a very simple ORM that provides **one-to-one**, **one-to-many**, **many-to-one**, **many-to-many** and **inverse** relationships on top of the [sqlite-net library](https://github.com/praeclarum/sqlite-net).

sqlite-net is an open source, minimal library to allow .NET and Mono applications to store data in [SQLite 3 databases](http://www.sqlite.org). SQLite-Net Extensions extends its funcionality to help the user handle relationships between sqlite-net entities.


#### How it works
SQLite-Net Extensions provides attributes for specifying the relationships in different ways and uses reflection to read and write the objects at Runtime.

SQLite-Net Extensions doesn't create any table or columns in your database and doesn't persist obscure elements to handle the database. For this reason, you have full control over the database schema used to persist your entities. SQLite-Net Extensions only requires you to specify the foreign keys used to handle the relationships and it will find out the rest by itself.

SQLite-Net Extensions doesn't modify or override any method behavior of SQLite.Net. Instead, it extends `SQLiteConnection` class with methods to handle the relationships.

For example `GetChildren` finds all the relationship properties that have been specified in the element, finds the required foreign keys and fills the properties automatically for you.

Complementarily `UpdateWithChildren` looks at the relationships that you have set, updates all the foreign keys and save the changes in the database. This is particulary helpful for one-to-many, many-to-many or one-to-one relationships where the foreign key is in the destination table.

You can update foreign keys manually if you feel more comfortable handling some relationships by yourself and let the SQLite-Net extensions handle the rest for you. You can even add or remove SQLite-Net extensions of any project at any time without changes to your database.

#### Some code
Theories are for the conspiracists, lets see some code. This is how you usually specify a relationship in **sqlite-net** (extracted from [sqlite-net wiki](https://github.com/praeclarum/sqlite-net/wiki/GettingStarted)):

    public class Stock    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        [MaxLength(8)]        public string Symbol { get; set; }    }    public class Valuation    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        [Indexed]        public int StockId { get; set; }        public DateTime Time { get; set; }        public decimal Price { get; set; }    }

Then you query like this:

    return db.Query<Valuation> ("select * from Valuation where StockId = ?", stock.Id);
    
With SQLite-Net extensions, no more need to write the queries manually, just specify the relationships in the entities:

    public class Stock    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        [MaxLength(8)]        public string Symbol { get; set; }        [OneToOne]      // One to one relationship with Valuation        public Valuation Valuation { get; set; }    }    public class Valuation    {        [PrimaryKey, AutoIncrement]        public int Id { get; set; }        [ForeignKey(typeof(Stock))]     // Specify the foreign key        public int StockId { get; set; }        public DateTime Time { get; set; }        public decimal Price { get; set; }        [OneToOne]      // One to one relationship with Valuation        public Stock Stock { get; set; }    }

SQLite-Net Extensions will find all the properties with a relationship attribute and then find the foreign keys and inverse attributes for them.

Here's how we'll create, read and update the entities:

    var db = new SQLiteConnection("database.sqlitedb");    db.CreateTable<Stock>();    db.CreateTable<Valuation>();    var euro = new Stock()        {            Symbol = "€"        };    db.Insert(euro);   // Insert the object in the database    var valuation = new Valuation()        {            Price = 15,            Time = DateTime.Now,        };    db.Insert(valuation);   // Insert the object in the database    // Objects created, let's stablish the relationship    euro.Valuation = valuation;    db.UpdateWithChildren(euro);   // Update the changes into the database    if (valuation.Stock == euro)    {        Debug.WriteLine("Inverse relationship already set, yay!");    }    // Get the object and the relationships    var storedValuation = db.GetWithChildren<Valuation>(valuation.Id);    if (euro.Symbol.Equals(storedValuation.Stock.Symbol))    {        Debug.WriteLine("Object and relationships loaded correctly!");    }
    
Ok, maybe that was a horrible example, because one stock may have many valuations, so it's not a one-to-one, but you get the point.


#### Limitations
Because of the way SQLite-Net Extensions conception and implementation, it has some limitations:

###### Only objects with ID can be used in a relationship
If you use `[AutoIncrement]` in your *Primary Key* this also means that you need to insert the object in the database first to be assigned a primary key.

Relationships are based in *foreign keys*. Foreign keys are just a reference to the primary key of another table. If the referenced object doesn't have an assigned primary key, there's no way to store this relationship in the database. We could *dectect* somehow if the object hasn't been persisted yet and then insert it to the database, but we would be losing control and again that is one of our main principles.

Before calling `UpdateWithChildren` make sure you have inserted all referenced objects in the database and you will be fine.

###### Inverse relationships are not loaded automatically on many-to-* relationships
Because of *many-to-many* and *many-to-one* relationships inverse may reference to **a lot** of objects, they have to be loaded manually using `GetChildren` method. Only *one-to-one* and *one-to-many* inverse relationships will be loaded automatically.

###### Foreign keys are not updated automatically for removed references
When you call to `UpdateWithChildren` method, it refreshes all the foreign keys based on the current relationships (including the inverse relationships), and stores the changes in the database. But if you remove a reference to an object, there's no way for SQLite-Net Extensions to know that you have an object in memory that wasn't referenced before. If you later call to `UpdateWithChildren` to the referenced object, you may find that the removed reference is unintentionally restored.

To keep a reference to the removed object it's recommended to reload the referenced object from the database.

#### I want [moar](http://slappersonly.com/wordpress/wp-content/uploads/2013/02/moar.jpg)!!
If you want to see some code in action, you can take a look at the Integration Tests. They are packed into a Xamarin.iOS project using NUnit Lite. Run it as a normal Xamarin.iOS App to execute the tests.

> Those are my principles. If you don't like them I have others.


We'd like to hear from you. Don't hesitate in contacting us for suggestions, feature requests, bug reports or anything you need.

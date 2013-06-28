using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions;

namespace SQLiteNetExtensions.Tests.Extensions
{
    public class DummyClassA
    {
        [OneToOne]
        public DummyClassB OneB { get; set; }

        [OneToMany]
        public List<DummyClassC> OneToManyC { get; set; }

        [ManyToMany(typeof(IntermediateDummyADummyD))]
        public DummyClassD[] ManyToManyD { get; set; }

        public int FooInt { get; set; }
        public string BarString { get; set; }
    }

    public class DummyClassB
    {
        [OneToOne]
        public DummyClassA OneA { get; set; }
    }

    public class DummyClassC
    {
        [ManyToOne(inverseProperty: "")]
        public List<DummyClassD> ManyToOneD { get; set; }
    }

    public class DummyClassD
    {
        
    }

    public class IntermediateDummyADummyD
    {
        
    }

    [TestFixture]
    public class PropertyExtensionsTests
    {
        [Test]
        public void TestOneToOneInverse()
        {
            var typeA = typeof (DummyClassA);
            var typeB = typeof (DummyClassB);

            var expectedAOneBProperty = typeA.GetProperty("OneB");
            var expectedBOneAProperty = typeB.GetProperty("OneA");

            var aOneBProperty = expectedBOneAProperty.GetInversePropertyForRelationship(typeB);
            var bOneAProperty = expectedAOneBProperty.GetInversePropertyForRelationship(typeA);

            Assert.AreEqual(expectedAOneBProperty, aOneBProperty, "Type A -> Type B inverse relationship is not correct");
            Assert.AreEqual(expectedBOneAProperty, bOneAProperty, "Type B -> Type A inverse relationship is not correct");
        }

        [Test]
        public void TestNoInverse()
        {
            var typeC = typeof(DummyClassC);

            var cManyDProperty = typeC.GetProperty("ManyToOneD");

            var inverseProperty = cManyDProperty.GetInversePropertyForRelationship(typeC);
            Assert.IsNull(inverseProperty, "Declared empty Inverse Property should be null");

        }

        [Test]
        public void TestOneToOneRelationShipAttribute()
        {
            var typeA = typeof (DummyClassA);
            var property = typeA.GetProperty("OneB");

            var expectedAttributeType = typeof (OneToOneAttribute);
            var attribute = property.GetAttribute<RelationshipAttribute>();
            var attributeType = attribute.GetType();

            Assert.AreEqual(expectedAttributeType, attributeType, "Relationship Attribute doesn't match expected type");
        }

        [Test]
        public void TestNoRelationShipAttribute()
        {
            var typeA = typeof(DummyClassA);
            var property = typeA.GetProperty("FooInt");

            var attribute = property.GetAttribute<RelationshipAttribute>();

            Assert.IsNull(attribute);
        }

        [Test]
        public void TestEntityTypeObject()
        {
            var typeA = typeof(DummyClassA);
            var property = typeA.GetProperty("OneB");
            var expectedType = typeof (DummyClassB);
            const EnclosedType expectedContainerType = EnclosedType.None;

            EnclosedType enclosedType;
            var entityType = property.GetEntityType(out enclosedType);

            Assert.AreEqual(expectedType, entityType);
            Assert.AreEqual(expectedContainerType, enclosedType);
        }

        [Test]
        public void TestEntityTypeArray()
        {
            var typeA = typeof(DummyClassA);
            var property = typeA.GetProperty("ManyToManyD");
            var expectedType = typeof(DummyClassD);
            const EnclosedType expectedContainerType = EnclosedType.Array;

            EnclosedType enclosedType;
            var entityType = property.GetEntityType(out enclosedType);

            Assert.AreEqual(expectedType, entityType);
            Assert.AreEqual(expectedContainerType, enclosedType);
        }

        [Test]
        public void TestEntityTypeList()
        {
            var typeA = typeof(DummyClassA);
            var property = typeA.GetProperty("OneToManyC");
            var expectedType = typeof(DummyClassC);
            const EnclosedType expectedContainerType = EnclosedType.List;

            EnclosedType enclosedType;
            var entityType = property.GetEntityType(out enclosedType);

            Assert.AreEqual(expectedType, entityType);
            Assert.AreEqual(expectedContainerType, enclosedType);
        }
    }
}

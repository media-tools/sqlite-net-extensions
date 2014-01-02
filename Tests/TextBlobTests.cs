using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions.TextBlob;

namespace Tests
{
    [TestFixture]
    public class TextBlobTests
    {
        private class ClassA
        {
            public string Foo { get; set; }

            public string ElementsBlobbed { get; set; }

            [TextBlob("ElementsBlobbed")]
            public List<string> Elements { get; set; }
        }


        [Test]
        public void TestUpdateTextProperty()
        {
            var obj = new ClassA
                {
                    Foo = "Foo String",
                    Elements = new List<string>
                        {
                            "Foo String 1",
                            "Foo String 2"
                        }
                };

            const string textValue = "Mock Serialized String";

            var mockSerializer = new Mock<ITextBlobSerializer>();
            TextBlobOperations.SetTextSerializer(mockSerializer.Object); // Override default JSON serializer

            var obj1 = obj;
            mockSerializer.Setup(serializer => serializer.Serialize(obj1.Elements)).Returns(() => textValue);
            
            TextBlobOperations.UpdateTextBlobProperty(obj, typeof(ClassA).GetProperty("Elements"));

            Assert.AreEqual(textValue, obj1.ElementsBlobbed);
        }

        [Test]
        public void TestGetTextProperty()
        {
            const string textValue = "Mock Serialized String";
            var values = new List<string>
                {
                    "Foo string 1",
                    "Foo string 2"
                };
            
            var obj = new ClassA
            {
                Foo = "Foo String",
                ElementsBlobbed = textValue
            };

            var mockSerializer = new Mock<ITextBlobSerializer>();
            TextBlobOperations.SetTextSerializer(mockSerializer.Object); // Override default JSON serializer

            var obj1 = obj;
            var elementsProperty = typeof (ClassA).GetProperty("Elements");
            mockSerializer.Setup(serializer => serializer.Deserialize(textValue, elementsProperty.PropertyType)).Returns(() => values);

            TextBlobOperations.GetTextBlobChild(obj, typeof(ClassA).GetProperty("Elements"));

            Assert.AreEqual(values, obj1.Elements);
        }
    }
}

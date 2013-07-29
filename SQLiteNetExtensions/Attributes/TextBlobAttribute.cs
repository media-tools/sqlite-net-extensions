using System;

namespace SQLiteNetExtensions.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class TextBlobAttribute : RelationshipAttribute
    {
        public TextBlobAttribute(string textProperty) : base(null, null, null, OnDeleteAction.None)
        {
            TextProperty = textProperty;
        }

        public string TextProperty { get; private set; }
    }
}

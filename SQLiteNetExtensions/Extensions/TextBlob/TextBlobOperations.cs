using System.Diagnostics;
using System.Reflection;
using SQLiteNetExtensions.Attributes;
using SQLiteNetExtensions.Extensions.TextBlob.Serializers;
#if USING_MVVMCROSS
using SQLiteConnection = Cirrious.MvvmCross.Plugins.Sqlite.ISQLiteConnection;
#else
using SQLite;
#endif


namespace SQLiteNetExtensions.Extensions.TextBlob
{
    public static class TextBlobOperations
    {
        private static ITextBlobSerializer _serializer;

        public static void SetTextSerializer(ITextBlobSerializer serializer)
        {
            _serializer = serializer;
        }

        public static ITextBlobSerializer GetTextSerializer()
        {
            // If not specified, use default JSON serializer
            return _serializer ?? (_serializer = new JsonBlobSerializer());
        }

        public static void GetTextBlobChild<T>(ref T element, PropertyInfo relationshipProperty)
        {
            var type = typeof(T);
            var relationshipType = relationshipProperty.PropertyType;

            Debug.Assert(relationshipType != typeof(string), "TextBlob property is already a string");

            var textblobAttribute = relationshipProperty.GetAttribute<TextBlobAttribute>();
            var textProperty = type.GetProperty(textblobAttribute.TextProperty, typeof (string));
            Debug.Assert(textProperty != null, "Text property for TextBlob relationship not found");

            var textValue = (string)textProperty.GetValue(element, null);
            var value = textValue != null ? GetTextSerializer().Deserialize(textValue, relationshipType) : null;

            relationshipProperty.SetValue(element, value, null);
        }

        public static void UpdateTextBlobProperty<T>(ref T element, PropertyInfo relationshipProperty)
        {
            var type = typeof(T);
            var relationshipType = relationshipProperty.PropertyType;

            Debug.Assert(relationshipType != typeof(string), "TextBlob property is already a string");

            var textblobAttribute = relationshipProperty.GetAttribute<TextBlobAttribute>();
            var textProperty = type.GetProperty(textblobAttribute.TextProperty, typeof(string));
            Debug.Assert(textProperty != null, "Text property for TextBlob relationship not found");

            var value = relationshipProperty.GetValue(element, null);
            var textValue = value != null ? GetTextSerializer().Serialize(value) : null;

            textProperty.SetValue(element, textValue, null);
        }
    }

}

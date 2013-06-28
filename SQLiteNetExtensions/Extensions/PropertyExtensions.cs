using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using SQLiteNetExtensions.Attributes;

namespace SQLiteNetExtensions.Extensions
{
    public enum EnclosedType
    {
        None,
        Array,
        List
    }

    public static class PropertyExtensions
    {
        public static RelationshipAttribute GetRelationShipAttribute(this PropertyInfo property)
        {
            RelationshipAttribute attribute = null;
            var relationshipAttributes = (RelationshipAttribute[])property.GetCustomAttributes(typeof(RelationshipAttribute), true);
            if (relationshipAttributes.Length > 0)
            {
                attribute = relationshipAttributes[0];
            }
            return attribute;
        }

        public static PropertyInfo GetForeignKeyPropertyForRelationship(this PropertyInfo relationshipProperty)
        {
            return null;
        }

        public static PropertyInfo GetInversePropertyForRelationship(this PropertyInfo property, Type elementType)
        {
            PropertyInfo result = null;

            EnclosedType enclosedType;
            var propertyType = property.GetEntityType(out enclosedType);

            var properties = propertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var inverseProperty in properties)
            {
                var attribute = inverseProperty.GetRelationShipAttribute();
                EnclosedType enclosedInverseType;
                var inverseType = inverseProperty.GetEntityType(out enclosedInverseType);
                if (attribute != null && elementType.IsAssignableFrom(inverseType))
                {
                    result = inverseProperty;
                }
            }

            return result;
        }

        public static Type GetEntityType(this PropertyInfo property, out EnclosedType enclosedType)
        {
            var type = property.PropertyType;
            enclosedType = EnclosedType.None;

            if (type.IsArray)
            {
                type = type.GetElementType();
                enclosedType = EnclosedType.Array;
            }
            else if (type.IsGenericType && typeof(List<>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                type = type.GetGenericArguments()[0];
                enclosedType = EnclosedType.List;
            }
            return type;
        }
    }

    
}

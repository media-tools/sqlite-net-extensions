using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static T GetAttribute<T>(this PropertyInfo property) where T : Attribute
        {
            T attribute = null;
            var attributes = (T[])property.GetCustomAttributes(typeof(T), true);
            if (attributes.Length > 0)
            {
                attribute = attributes[0];
            }
            return attribute;
        }

        public static PropertyInfo GetForeignKeyPropertyForRelationship(this PropertyInfo relationshipProperty)
        {
            return null;
        }

        public static PropertyInfo GetInversePropertyForRelationship(this PropertyInfo property, Type elementType)
        {

            var attribute = property.GetAttribute<ReversibleRelationshipAttribute>();
            if (attribute == null|| attribute.InverseProperty.Equals(""))
            {
                // Relationship not reversible
                return null;
            }

            EnclosedType enclosedType;
            var propertyType = property.GetEntityType(out enclosedType);

            PropertyInfo result = null;
            if (attribute.InverseProperty != null)
            {
                result = propertyType.GetProperty(attribute.InverseProperty);
            }
            else
            {
                var properties = propertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var inverseProperty in properties)
                {
                    var inverseAttribute = inverseProperty.GetAttribute<ReversibleRelationshipAttribute>();
                    EnclosedType enclosedInverseType;
                    var inverseType = inverseProperty.GetEntityType(out enclosedInverseType);
                    if (inverseAttribute != null && elementType.IsAssignableFrom(inverseType))
                    {
                        result = inverseProperty;
                    }
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

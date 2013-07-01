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

    public class ManyToManyMetaInfo
    {
        public Type IntermediateTable { get; set; }
        public PropertyInfo OriginProperty { get; set; }
        public PropertyInfo DestinationProperty { get; set; }
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

        private static PropertyInfo GetExplicitForeignKeyProperty(this Type type, Type destinationType)
        {
            return (from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    let foreignKeyAttribute = property.GetAttribute<ForeignKeyAttribute>()
                    where foreignKeyAttribute != null && foreignKeyAttribute.ForeignType.IsAssignableFrom(destinationType)
                    select property)
                        .FirstOrDefault();
        }

        private static PropertyInfo GetConventionForeignKeyProperty(this Type type, string destinationTypeName)
        {
            var conventionFormats = new List<string> { "{0}Id", "{0}Key", "{0}ForeignKey" };

            var conventionNames = conventionFormats.Select(format => string.Format(format, destinationTypeName)).ToList();

            // No explicit declaration, search for convention names
            return (from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    where conventionNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase)
                    select property)
                        .FirstOrDefault();
        }

        public static PropertyInfo GetForeignKeyProperty(this Type type,
                                                                         PropertyInfo relationshipProperty, Type intermediateType = null, bool inverse = false)
        {
            PropertyInfo result;
            var attribute = relationshipProperty.GetAttribute<RelationshipAttribute>();

            EnclosedType enclosedType;
            var propertyType = relationshipProperty.GetEntityType(out enclosedType);

            var originType = intermediateType ?? (inverse ? propertyType : type);
            var destinationType = inverse ? type : propertyType;
            
            if (!string.IsNullOrEmpty(attribute.ForeignKey))
            {
                // Explicitly declared foreing key name
                result = originType.GetProperty(attribute.ForeignKey);
            }
            else
            {
                // Explicitly declared attribute
                result = originType.GetExplicitForeignKeyProperty(destinationType) ??
                    originType.GetConventionForeignKeyProperty(destinationType.Name);
            }

            return result;
        }


        public static PropertyInfo GetInverseProperty(this Type elementType, PropertyInfo property)
        {

            var attribute = property.GetAttribute<RelationshipAttribute>();
            if (attribute == null || (attribute.InverseProperty != null && attribute.InverseProperty.Equals("")))
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
                    var inverseAttribute = inverseProperty.GetAttribute<RelationshipAttribute>();
                    EnclosedType enclosedInverseType;
                    var inverseType = inverseProperty.GetEntityType(out enclosedInverseType);
                    if (inverseAttribute != null && elementType.IsAssignableFrom(inverseType))
                    {
                        result = inverseProperty;
                        break;
                    }
                }
            }

            return result;
        }


        public static ManyToManyMetaInfo GetManyToManyMetaInfo(this Type type, PropertyInfo relationship)
        {
            var manyToManyAttribute = relationship.GetAttribute<ManyToManyAttribute>();
            Debug.Assert(manyToManyAttribute != null, "Unable to find ManyToMany attribute");

            var inverseProperty = type.GetInverseProperty(relationship);
            Debug.Assert(inverseProperty != null, "Inverse relationship is required");

            EnclosedType inverseEnclosedType;
            var inverseManyToManyAttrute = inverseProperty.GetAttribute<ManyToManyAttribute>();
            Debug.Assert(inverseManyToManyAttrute != null, "Unable to find ManyToMany attribute");
            Debug.Assert(type == inverseProperty.GetEntityType(out inverseEnclosedType), "Inverse relationship type doesn't match");
            Debug.Assert(inverseEnclosedType != EnclosedType.None, "N:M relationship must be an Array or List type");
            Debug.Assert(inverseManyToManyAttrute.IntermediateTable == manyToManyAttribute.IntermediateTable, "Inverse intermediate type doesn't match");
           
            var intermediateType = manyToManyAttribute.IntermediateTable;
            Debug.Assert(intermediateType != null, "Intermediate table cannot be null");

            var originKeyProperty = type.GetForeignKeyProperty(relationship, intermediateType);
            var inverseKeyProperty = type.GetForeignKeyProperty(relationship, intermediateType, true);

            Debug.Assert(originKeyProperty != null, "Unable to find Foreign key in intermediate type");
            Debug.Assert(originKeyProperty != null, "Unable to find inverse Foreign key in intermediate table");

            return new ManyToManyMetaInfo
                {
                    IntermediateTable = intermediateType,
                    OriginProperty = originKeyProperty,
                    DestinationProperty = inverseKeyProperty
                };
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

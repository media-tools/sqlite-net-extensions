using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SQLiteNetExtensions.Attributes;

#if USING_MVVMCROSS
using Cirrious.MvvmCross.Plugins.Sqlite;
#else
using SQLite;
#endif

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
        public Type IntermediateType { get; set; }
        public PropertyInfo OriginProperty { get; set; }
        public PropertyInfo DestinationProperty { get; set; }
    }

    public static class ReflectionExtensions
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

        public static PropertyInfo GetForeignKeyProperty(this Type type, PropertyInfo relationshipProperty, Type intermediateType = null, bool inverse = false)
        {
            PropertyInfo result;
            var attribute = relationshipProperty.GetAttribute<RelationshipAttribute>();
            RelationshipAttribute inverseAttribute = null;

            EnclosedType enclosedType;
            var propertyType = relationshipProperty.GetEntityType(out enclosedType);

            var originType = intermediateType ?? (inverse ? propertyType : type);
            var destinationType = inverse ? type : propertyType;

            // Inverse relationships may have the foreign key declared in the inverse property relationship attribute
            var inverseProperty = type.GetInverseProperty(relationshipProperty);
            if (inverseProperty != null)
            {
                inverseAttribute = inverseProperty.GetAttribute<RelationshipAttribute>();
            }

            if (!inverse && !string.IsNullOrEmpty(attribute.ForeignKey))
            {
                // Explicitly declared foreign key name
                result = originType.GetProperty(attribute.ForeignKey);
            }
            else if (!inverse && inverseAttribute != null && !string.IsNullOrEmpty(inverseAttribute.InverseForeignKey))
            {
                // Explicitly declared inverse foreign key name in inverse property (double inverse refers to current entity foreign key)
                result = originType.GetProperty(inverseAttribute.InverseForeignKey);
            }
            else if (inverse && !string.IsNullOrEmpty(attribute.InverseForeignKey))
            {
                // Explicitly declared inverse foreign key name
                result = originType.GetProperty(attribute.InverseForeignKey);
            }
            else if (inverse && inverseAttribute != null && !string.IsNullOrEmpty(inverseAttribute.ForeignKey))
            {
                // Explicitly declared foreign key name in inverse property
                result = originType.GetProperty(inverseAttribute.ForeignKey);
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

            var intermediateType = manyToManyAttribute.IntermediateType;
            var destinationKeyProperty = type.GetForeignKeyProperty(relationship, intermediateType);
            var inverseKeyProperty = type.GetForeignKeyProperty(relationship, intermediateType, true);

            return new ManyToManyMetaInfo
                {
                    IntermediateType = intermediateType,
                    OriginProperty = inverseKeyProperty,
                    DestinationProperty = destinationKeyProperty
                };
        }

        public static List<PropertyInfo> GetRelationshipProperties(this Type type)
        {
            return (from property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    where property.GetAttribute<RelationshipAttribute>() != null
                    select property).ToList();
        } 

        public static PropertyInfo GetPrimaryKey(this Type type)
        {
            return (from propety in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    where propety.GetAttribute<PrimaryKeyAttribute>() != null
                    select propety).FirstOrDefault();
        } 
        
    }

    
}
